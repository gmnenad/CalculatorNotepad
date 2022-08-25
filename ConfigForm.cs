using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Numbers;

namespace CalculatorNotepad
{
    public partial class ConfigForm : Form
    {
        public mcConfig cfg { get; set; }

        public ConfigForm()
        {
            cfg = mc.cfg.makeClone();
            InitializeComponent();
            // version
            appVersion.Text="ver "+ Assembly.GetExecutingAssembly().GetName().Version;
            //appVersion.Text = "ver " + Assembly.GetEntryAssembly().GetName().Version;
            // copy from struct to form
            cbCase.Items.Add("Insensitive :  can use 'SIN' instead of 'sin'. But can not define new SIN(x)");
            cbCase.Items.Add("Sensitive   :  can not use 'SIN', must type exactly 'sin'. But can define new SIN(x) ");
            cbCase.Items.Add("Dynamic     :  can use 'SIN' instead of 'sin' until new SIN(x) is defined");
            // basics
            ePresetFile.Text = cfg.PresetFile;
            cbLoadLast.Checked = cfg.openLastOnStart;
            cbAutoLastResult.Checked = cfg.autoLastResultAppend;
            cbReplaceKnownSymbols.Checked = cfg.replaceKnownSymbols;
            cbCase.SelectedIndex = (int)cfg.sensitivity;
            // timeouts
            eTimeoutFunc.Text = cfg.timeoutFuncMs.ToString();
            eTimeoutDoc.Text = (cfg.timeoutDocMs / 1000).ToString();
            cbTimeoutDisabled.Checked = cfg.timeoutDisabled;
            cbShowExecutionTime.Checked = cfg.showExecutionTime;
            // advanced
            cbAllowFunctionRedefinition.Checked = cfg.allowFuncRedefinition;
            cbAllowBuiltinRedefinition.Checked = cfg.allowBuiltInRedefinition;
            cbAllowBuiltinRedefinition.Enabled = cfg.allowFuncRedefinition;
            cbNativeExponent.Checked = cfg.isBinHexExponentNative;
            cbDisplayBinHexFloat.Checked = cfg.displayBinHexFloat;
            eDisableMask.Text = cfg.disabledOptimizations != 0 ? ((int)cfg.disabledOptimizations).ToString() : "";
            eAutocompleteChars.Text = cfg.autocompleteChars.ToString();
            eDebugVars.Text = cfg.DebugVars;
            eFormatDecimals.Text = cfg.resFormatDecimals < 0 ? "" : cfg.resFormatDecimals.ToString();
            eFormatSeparator.Text = cfg.resFormatSeparator;
            eFractionSeparator.Text = cfg.resFractionSeparator;
            cbAutoFocusError.Checked = cfg.autoFocusError;
            setPrecisionCombos(Enum.GetValues<NumberClass>().ToList().IndexOf(cfg.numberType));
            if (cfg.numberPrecision < 52)  cfg.numberPrecision = 52;
            eRealPrecision.Text = cfg.numberPrecision.ToString();
            // tooltips
            createToolTips(panel1.Controls);
            createToolTips(panel2.Controls);
        }

        private void ConfigForm_Load(object sender, EventArgs e)
        {

        }


        private void ConfigForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            // ** copy from form to struct
            // basics
            cfg.PresetFile= ePresetFile.Text;
            cfg.openLastOnStart= cbLoadLast.Checked;
            cfg.autoLastResultAppend= cbAutoLastResult.Checked;
            cfg.replaceKnownSymbols= cbReplaceKnownSymbols.Checked;
            cfg.sensitivity = (mcCaseSensitivity)cbCase.SelectedIndex;
            // timeouts
            long parsedTime;
            if (long.TryParse(eTimeoutFunc.Text, out parsedTime) && (parsedTime > 0)) cfg.timeoutFuncMs = parsedTime;
            if (long.TryParse(eTimeoutDoc.Text, out parsedTime) && (parsedTime > 0)) cfg.timeoutDocMs = parsedTime * 1000;
            if (cfg.timeoutFuncMs > cfg.timeoutDocMs) cfg.timeoutFuncMs = cfg.timeoutDocMs;
            cfg.timeoutDisabled= cbTimeoutDisabled.Checked;
            cfg.showExecutionTime= cbShowExecutionTime.Checked;
            // advanced
            cfg.allowFuncRedefinition = cbAllowBuiltinRedefinition.Enabled;
            cfg.allowBuiltInRedefinition= cbAllowBuiltinRedefinition.Checked;
            cfg.isBinHexExponentNative= cbNativeExponent.Checked;
            cfg.displayBinHexFloat= cbDisplayBinHexFloat.Checked;
            int tmpInt;
            if (int.TryParse(eDisableMask.Text, out tmpInt)) cfg.disabledOptimizations = (mcOptimization)tmpInt; else cfg.disabledOptimizations = mcOptimization.None;
            if (int.TryParse(eAutocompleteChars.Text, out tmpInt))  cfg.autocompleteChars = tmpInt;
            cfg.DebugVars=eDebugVars.Text;
            if (int.TryParse(eFormatDecimals.Text, out tmpInt)) cfg.resFormatDecimals = tmpInt; else cfg.resFormatDecimals = -1;
            cfg.resFormatSeparator = eFormatSeparator.Text ;
            cfg.resFractionSeparator = eFractionSeparator.Text;
            cfg.autoFocusError=cbAutoFocusError.Checked;
            cfg.numberType = Enum.GetValues<NumberClass>()[cbRealType.SelectedIndex] ;
            if (int.TryParse(eRealPrecision.Text, out tmpInt)) cfg.numberPrecision = Math.Max(52,Math.Min(4096,tmpInt));
            Number.defaultClassType = cfg.numberType;
            Number.defaultPrecision = cfg.numberPrecision;
            // ** save struct
            var oldCfg = mc.cfg.makeClone();
            mc.cfg = cfg.makeClone();
            mc.cfg.Save();
        }

        // create tooltip for every child control that has string in TAG
        void createToolTips(Control.ControlCollection controls)
        {
            foreach( var co in controls)
                if (co is Control)
                {
                    // check for this control
                    var c = co as Control;
                    if ((c.Tag!=null)&&((string)c.Tag != ""))
                    {
                        ToolTip tt = new ToolTip();
                        // Set up the delays for the ToolTip.
                        tt.AutomaticDelay = 300;
                        tt.AutoPopDelay = 50000;
                        tt.ShowAlways = true;
                        tt.SetToolTip(c, (string)c.Tag);
                    }
                }
        }


        private void pbSelectFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            dlg.Filter = "txt files (*.txt)|*.txt|Calc files (*.calc)|*.calc|All files (*.*)|*.*";
            dlg.FilterIndex = 1;
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                // try to get relative filename path
                string fileName = dlg.FileName;
                string exePath = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
                string path = System.IO.Path.GetDirectoryName(fileName).TrimEnd('\\');
                if (path.SubStr(0, exePath.Length) == exePath)
                    fileName = fileName.SubStr(exePath.Length).TrimStart('\\');
                // dtore filename
                ePresetFile.Text = fileName;
                cfg.PresetFile = fileName;
            }
        }

        private void eTimeoutFunc_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void cbAllowFunctionRedefinition_CheckedChanged(object sender, EventArgs e)
        {
            cbAllowBuiltinRedefinition.Checked = false;
            cbAllowBuiltinRedefinition.Enabled = cbAllowFunctionRedefinition.Checked;
        }


        void setPrecisionCombos(int realType)
        {
            if (realType < 0 || realType> 2) realType = 0;
            if (cbRealType.SelectedIndex != realType)
                cbRealType.SelectedIndex = realType;
            eRealPrecision.Text = realType switch
            {
                1 => "127", // MPFR
                2 => "64",  // Quad
                _ => "53"   // Double
            };
            eRealPrecision.Enabled = realType == 1;
            eExpBits.Text = realType switch
            {
                1 => "32",  // MPFR
                2 => "64",  // Quad
                _ => "11"   // Double
            };
        }

        private void cbRealType_SelectedIndexChanged(object sender, EventArgs e) => setPrecisionCombos(cbRealType.SelectedIndex);

    }
}
