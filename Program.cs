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
                // check if DLLs are in same folder as EXE, if not raise exception that will be more readable
                foreach(var d in new string[] { "libgmp-10.dll", "libmpfr-6.dll" })
                if (!File.Exists(d))
                    throw new FileNotFoundException(d+" must be in the same folder as EXE !");

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