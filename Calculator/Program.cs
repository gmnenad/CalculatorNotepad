using System.Diagnostics;

namespace CalculatorNotepad
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                // check if DLLs are in correct folders relative to EXE, if not raise exception that will be more readable
                foreach(var d in new string[] { Mpfr.Gmp.gmp_lib.libgmp10dll, Mpfr.Native.mpfr_lib.libmpfr6dll })
                if (!File.Exists(d))
                    throw new FileNotFoundException(d+" not found in correct folder under EXE folder !");

                ApplicationConfiguration.Initialize();
                Application.Run(new FormCalculator());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message+Environment.NewLine+ ex.InnerException+ex.StackTrace);
                throw;
            }
        }
    }
}