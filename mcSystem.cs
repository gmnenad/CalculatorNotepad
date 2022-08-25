using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;


namespace CalculatorNotepad
{

    /*
      
        Since StackOverflow exceptions are not manageable, this class allows tracking of free stack space and raising exception ( or gracefully aborting) before stack really overflow. 
        NOTE: only use if you really need large stack and can not algorithmically avoid need by using  Stack<T> or dynamic programming.

        Example usage:
            // using by letting it throw an exception
            StackGuard sg; 
            int fib1(int x) => x<=1 || sg.checkStackOverflow()? x : fib1(x-1)+fib1(x-1);
            sg = new StackGuard();
            try{
                var res= fib(10000); 
            }catch{
                Console.WriteLine("Handled exception, before StackOverflow crashed app ");
            }
            // using by returning error result on imminent overflow, and also without static variable (using wrapper function)
            int fib(int x){
                StackGuard sg = new StackGuard();
                int fib_inner(int x){
                    if (sg.checkStackOverflow()) return -1;
                    return x<=1 ? x : fib_inner(x-1) + fib_inner(x-1);
                }
                return fib_inner(x);
            }
            Console.WriteLine("res="+fib(10000)+" ( if -1 it was stack overflow )");
            
            
        
        In addition, if stack is an issue, it is possible to increase stack size for c# .NET app:
            - install EDITBIN.EXE  :  AddRemove programs -> Visual Studio -> Modify -> Individual components -> MSVC <latest> x64/x86 build tools
            - it will be installed in eg "C:\Program Files\Microsoft Visual Studio\2022\Preview\VC\Tools\MSVC\14.33.31517\bin\Hostx64\x64\EDITBIN.EXE"
            - edit config field :  Visual Studio -> right click project -> Properties -> Build -> Events -> Post Build Events field
            - set it to : "C:\Program Files\Microsoft Visual Studio\2022\Preview\VC\Tools\MSVC\14.33.31517\bin\Hostx64\x64\EDITBIN.EXE" /stack:4000000 "$(TargetDir)$(TargetName).exe"
            - where 4000000 is 4M stack ( default is 1.5M , max is 'infinite' but >100M will have problems finding continuous space )
             
     
    */

    /// <summary>
    /// Guard against stack overflows, can warn before it happens
    /// </summary>
    public class StackGuard
    {
        // parameters
        bool throwException; // should exception be thrown on trigger
        // internal counters
        long stackGuard0; // stack value at start (depth 0)
        long stackLastFree; // last free stack size
        long nextCheckCall; // at which call to do next free size check
        // results and/or counters
        public long stackCountCall; // stack count for each function, that is not rolled back by function returns
        public long stackCountDepth; // highest stackCount seen - since fib(x-1)+fib(x-2) can break on '+' with stackCount==1
        public double stackAvgPerCall; // average bytes per call used on stack
        public double stackAvgPerDepth; // average bytes per depth used on stack
        public string? ErrorMessage; // error message for exception/trigger, or null if no overflow so far
        // static variables
        private static int pageSize = -1;


        /// <summary>
        /// Create guard that will warn when stack gets under specified free size ( default zero already include small safety net )
        /// Default behaviour is to throw exception on imminent overflow ( which is manageable, unlike real StackOverflow exception )
        /// </summary>
        /// <param name="throwException">default true will throw exception when overflow is imminent</param>
        /// <param name="first_check_after">after how many calls to do first stack size chack ( to reduce performance impact )</param>
        public StackGuard(bool throw_exception=true)
        {
            resetCounters(throw_exception);
        }


        public void resetCounters(bool? throw_exception = null)
        {
            stackGuard0 = getFreeStackSize();
            stackLastFree = long.MaxValue;
            stackAvgPerCall = stackAvgPerDepth = 0;
            stackCountCall = stackCountDepth = 0;
            ErrorMessage = null;
            nextCheckCall = 3;
            if(throw_exception!=null)
                throwException = throw_exception==true;
        }

        /// <summary>
        /// Return approximate free stack size, in bytes. Fairly fast, ~1us (1e-6s) on avg PC
        /// </summary>
        public unsafe static long getFreeStackSize()
        {
            try
            {
                if (pageSize < 0)
                {
                    var sysInfo = new SYSTEM_INFO();
                    GetSystemInfo(out sysInfo);
                    pageSize = (int)sysInfo.dwPageSize;
                }
                var stackInfo = new MEMORY_BASIC_INFORMATION();
                IntPtr currentAddr = new IntPtr(&stackInfo);
                VirtualQuery(currentAddr, ref stackInfo, sizeof(MEMORY_BASIC_INFORMATION));
                long res = (Int64)currentAddr - stackInfo.AllocationBase - pageSize * 16;
                return res;
            }
            catch
            {
                return -1; // since negative values trigger stack error report
            }
        }


        /// <summary>
        /// Return true if stack overflow is imminent (see ErrorMessage), or raise exception if default constructor parameter used.
        /// Optional 'stackDepth' parameter if app is also counting stack depths per separate functions that call this .
        /// Function dynamically decide when to test stack, to minimize performance impact ( on avg ~300ns per call, 3x faster than repeat calls to getFreeStackSize).
        /// It also dynamically determine when to trigger warning based on avg stack usage per call ( when less than estimated 5 new calls can be made )
        /// </summary>
        /// <param name="stackDepth">optional parameter if multiple functions keep their own depth counters and will call this same instance method</param>
        public bool checkStackOverflow(int stackDepth=0)
        {
            stackCountCall++;
            if (stackDepth > stackCountDepth) // highest seen user depth
                stackCountDepth = stackDepth;
            if (stackCountCall >= nextCheckCall )
            {
                stackLastFree = getFreeStackSize(); // check free stack memory
                stackAvgPerCall = (stackGuard0 - stackLastFree) / (double)stackCountCall; // determine average stack size per call so far
                var remainsCalls = stackLastFree/ stackAvgPerCall; // how many average calls could be done before stack overflow
                if (remainsCalls < 100) // if close to end, check every next call
                {
                    if (stackCountDepth > 0) // if user supplied depths per function, we can see if some function uses more bytes
                    {
                        stackAvgPerDepth = (stackGuard0 - stackLastFree) / (double)stackCountDepth; // average bytes per user calls, can be higher than stackAvgPerCall
                        var remainsUserCalls = stackLastFree / stackAvgPerCall; // how many average calls could be done before stack overflow
                        if (remainsUserCalls < remainsCalls) // check trigger using smallest of two ( ie if avgPerCall=100 but avgPerDepth=1000, it means single user func can use 1000bytes, so can have less userCalls )
                            remainsCalls = remainsUserCalls;
                    }
                    //  trigger warning if less than 5 new calls could be made
                    if (remainsCalls < 5) 
                    {
                        ErrorMessage = "Stack Overflow imminent after  " + stackCountCall.ToString("N0") + " calls (" + stackAvgPerCall.ToString("N0") + " bytes/call)";
                        if (stackCountDepth > 0) // calculate average stack use per user function depth
                            ErrorMessage += ", at max depth " + stackCountDepth.ToString("N0") + " ( " + stackAvgPerDepth.ToString("N0") + " bytes/depth) ";
                        if (throwException)
                            throw new OverflowException(ErrorMessage);
                        else
                            return true;
                    }
                }
                else
                    nextCheckCall = stackCountCall + 1 + (int)(remainsCalls / 3) ; // next check after 3rd of free stack is used
            }
            return false;
        }


        // Kernel32 structs and external functions, with fixed size structs

        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        private struct MEMORY_BASIC_INFORMATION
        {
            public Int64  BaseAddress;           // PVOID
            public Int64  AllocationBase;        // PVOID
            public UInt32 AllocationProtect;     // DWORD
            public UInt16 PartitionId;           // WORD
            public UInt64 RegionSize;            // SIZE_T
            public UInt32 State;                 // DWORD
            public UInt32 Protect;               // DWORD
            public UInt32 Type;                  // DWORD
        }

        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        private struct SYSTEM_INFO
        {
            public UInt32 dwOemId;                          // DWORD
            public UInt32 dwPageSize;                       // DWORD
            public UInt64 lpMinimumApplicationAddress;      // LPVOID 
            public UInt64 lpMaximumApplicationAddress;      // LPVOID
            public UInt64 dwActiveProcessorMask;            // DWORD_PT
            public UInt32 dwNumberOfProcessors;             // DWORD
            public UInt32 dwProcessorType;                  // DWORD
            public UInt32 dwAllocationGranularity;          // DWORD
            public UInt16 wProcessorLevel;                  // WORD
            public UInt16 wProcessorRevision;               // WORD
        }

        [DllImport("kernel32.dll")]
        private static extern int VirtualQuery(IntPtr lpAddress, ref MEMORY_BASIC_INFORMATION lpBuffer, int dwLength);

        [DllImport("kernel32.dll")]
        private static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);


    }


}
