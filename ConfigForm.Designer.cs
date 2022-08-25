namespace CalculatorNotepad
{
    partial class ConfigForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.cbCase = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.cbReplaceKnownSymbols = new System.Windows.Forms.CheckBox();
            this.cbAutoLastResult = new System.Windows.Forms.CheckBox();
            this.cbLoadLast = new System.Windows.Forms.CheckBox();
            this.pbSelectFile = new System.Windows.Forms.PictureBox();
            this.ePresetFile = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.eFractionSeparator = new System.Windows.Forms.TextBox();
            this.label19 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.eExpBits = new System.Windows.Forms.TextBox();
            this.label17 = new System.Windows.Forms.Label();
            this.eRealPrecision = new System.Windows.Forms.TextBox();
            this.label16 = new System.Windows.Forms.Label();
            this.cbRealType = new System.Windows.Forms.ComboBox();
            this.label15 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.eDisableMask = new System.Windows.Forms.TextBox();
            this.cbAutoFocusError = new System.Windows.Forms.CheckBox();
            this.eFormatSeparator = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.eFormatDecimals = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.eDebugVars = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.eAutocompleteChars = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.cbDisplayBinHexFloat = new System.Windows.Forms.CheckBox();
            this.cbNativeExponent = new System.Windows.Forms.CheckBox();
            this.cbAllowBuiltinRedefinition = new System.Windows.Forms.CheckBox();
            this.cbAllowFunctionRedefinition = new System.Windows.Forms.CheckBox();
            this.cbShowExecutionTime = new System.Windows.Forms.CheckBox();
            this.cbTimeoutDisabled = new System.Windows.Forms.CheckBox();
            this.label6 = new System.Windows.Forms.Label();
            this.eTimeoutDoc = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.eTimeoutFunc = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.appVersion = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbSelectFile)).BeginInit();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.label1.Location = new System.Drawing.Point(14, 10);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(111, 21);
            this.label1.TabIndex = 0;
            this.label1.Text = "Basic options";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.label2.Location = new System.Drawing.Point(14, 276);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(148, 21);
            this.label2.TabIndex = 1;
            this.label2.Text = "Advanced options";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.cbCase);
            this.panel1.Controls.Add(this.label7);
            this.panel1.Controls.Add(this.cbReplaceKnownSymbols);
            this.panel1.Controls.Add(this.cbAutoLastResult);
            this.panel1.Controls.Add(this.cbLoadLast);
            this.panel1.Controls.Add(this.pbSelectFile);
            this.panel1.Controls.Add(this.ePresetFile);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Location = new System.Drawing.Point(19, 38);
            this.panel1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(979, 234);
            this.panel1.TabIndex = 2;
            // 
            // cbCase
            // 
            this.cbCase.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.cbCase.FormattingEnabled = true;
            this.cbCase.Location = new System.Drawing.Point(298, 164);
            this.cbCase.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cbCase.Name = "cbCase";
            this.cbCase.Size = new System.Drawing.Size(661, 23);
            this.cbCase.TabIndex = 13;
            this.cbCase.Tag = "Variable names are always case sensitive, although Autocomplete will help with ex" +
    "act casing  of both variables and functions.";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label7.Location = new System.Drawing.Point(14, 165);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(237, 18);
            this.label7.TabIndex = 12;
            this.label7.Text = "Case sensitivity for function names";
            // 
            // cbReplaceKnownSymbols
            // 
            this.cbReplaceKnownSymbols.AutoSize = true;
            this.cbReplaceKnownSymbols.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.cbReplaceKnownSymbols.Location = new System.Drawing.Point(18, 122);
            this.cbReplaceKnownSymbols.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cbReplaceKnownSymbols.Name = "cbReplaceKnownSymbols";
            this.cbReplaceKnownSymbols.Size = new System.Drawing.Size(558, 22);
            this.cbReplaceKnownSymbols.TabIndex = 11;
            this.cbReplaceKnownSymbols.Tag = "Alternative is to manually select symbols with Autocomplete->select eg. ∑ instead" +
    " of \'sum\' ";
            this.cbReplaceKnownSymbols.Text = "Automatically replace (sum,sqrt,>=,==,...) to known symbols (√∫∏∑≤≥≡≠→℮‼π∩)";
            this.cbReplaceKnownSymbols.UseVisualStyleBackColor = true;
            // 
            // cbAutoLastResult
            // 
            this.cbAutoLastResult.AutoSize = true;
            this.cbAutoLastResult.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.cbAutoLastResult.Location = new System.Drawing.Point(18, 90);
            this.cbAutoLastResult.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cbAutoLastResult.Name = "cbAutoLastResult";
            this.cbAutoLastResult.Size = new System.Drawing.Size(632, 22);
            this.cbAutoLastResult.TabIndex = 10;
            this.cbAutoLastResult.Text = "Automatically use last result, as left operand,  on new line if simple operation " +
    "(+-*/^...) is used";
            this.cbAutoLastResult.UseVisualStyleBackColor = true;
            // 
            // cbLoadLast
            // 
            this.cbLoadLast.AutoSize = true;
            this.cbLoadLast.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.cbLoadLast.Location = new System.Drawing.Point(18, 58);
            this.cbLoadLast.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cbLoadLast.Name = "cbLoadLast";
            this.cbLoadLast.Size = new System.Drawing.Size(267, 22);
            this.cbLoadLast.TabIndex = 9;
            this.cbLoadLast.Tag = "Also automatically saves working document on exit, and every 10sec.";
            this.cbLoadLast.Text = "Load last working document on start";
            this.cbLoadLast.UseVisualStyleBackColor = true;
            // 
            // pbSelectFile
            // 
            this.pbSelectFile.Image = global::CalculatorNotepad.Properties.Resources.Open_20;
            this.pbSelectFile.Location = new System.Drawing.Point(936, 17);
            this.pbSelectFile.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.pbSelectFile.Name = "pbSelectFile";
            this.pbSelectFile.Size = new System.Drawing.Size(23, 23);
            this.pbSelectFile.TabIndex = 2;
            this.pbSelectFile.TabStop = false;
            this.pbSelectFile.Click += new System.EventHandler(this.pbSelectFile_Click);
            // 
            // ePresetFile
            // 
            this.ePresetFile.Location = new System.Drawing.Point(106, 17);
            this.ePresetFile.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.ePresetFile.Name = "ePresetFile";
            this.ePresetFile.Size = new System.Drawing.Size(820, 23);
            this.ePresetFile.TabIndex = 1;
            this.ePresetFile.Tag = "File that will preload on start and can contain user defined constants variables," +
    " functions, units  and c# functions.";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label3.Location = new System.Drawing.Point(14, 16);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(73, 18);
            this.label3.TabIndex = 0;
            this.label3.Text = "Preset file";
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.eFractionSeparator);
            this.panel2.Controls.Add(this.label19);
            this.panel2.Controls.Add(this.label18);
            this.panel2.Controls.Add(this.eExpBits);
            this.panel2.Controls.Add(this.label17);
            this.panel2.Controls.Add(this.eRealPrecision);
            this.panel2.Controls.Add(this.label16);
            this.panel2.Controls.Add(this.cbRealType);
            this.panel2.Controls.Add(this.label15);
            this.panel2.Controls.Add(this.label13);
            this.panel2.Controls.Add(this.eDisableMask);
            this.panel2.Controls.Add(this.cbAutoFocusError);
            this.panel2.Controls.Add(this.eFormatSeparator);
            this.panel2.Controls.Add(this.label11);
            this.panel2.Controls.Add(this.eFormatDecimals);
            this.panel2.Controls.Add(this.label12);
            this.panel2.Controls.Add(this.eDebugVars);
            this.panel2.Controls.Add(this.label10);
            this.panel2.Controls.Add(this.label9);
            this.panel2.Controls.Add(this.eAutocompleteChars);
            this.panel2.Controls.Add(this.label8);
            this.panel2.Controls.Add(this.cbDisplayBinHexFloat);
            this.panel2.Controls.Add(this.cbNativeExponent);
            this.panel2.Controls.Add(this.cbAllowBuiltinRedefinition);
            this.panel2.Controls.Add(this.cbAllowFunctionRedefinition);
            this.panel2.Controls.Add(this.cbShowExecutionTime);
            this.panel2.Controls.Add(this.cbTimeoutDisabled);
            this.panel2.Controls.Add(this.label6);
            this.panel2.Controls.Add(this.eTimeoutDoc);
            this.panel2.Controls.Add(this.label5);
            this.panel2.Controls.Add(this.eTimeoutFunc);
            this.panel2.Controls.Add(this.label4);
            this.panel2.Location = new System.Drawing.Point(19, 303);
            this.panel2.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(979, 452);
            this.panel2.TabIndex = 3;
            // 
            // eFractionSeparator
            // 
            this.eFractionSeparator.Location = new System.Drawing.Point(716, 347);
            this.eFractionSeparator.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.eFractionSeparator.Name = "eFractionSeparator";
            this.eFractionSeparator.Size = new System.Drawing.Size(33, 23);
            this.eFractionSeparator.TabIndex = 34;
            this.eFractionSeparator.Tag = "If result is 12345678.9 and separator is \',\' then result is shown as  12,345,678." +
    "9";
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label19.Location = new System.Drawing.Point(566, 347);
            this.label19.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(152, 18);
            this.label19.TabIndex = 33;
            this.label19.Text = "and fraction separator";
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label18.Location = new System.Drawing.Point(931, 380);
            this.label18.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(31, 18);
            this.label18.TabIndex = 32;
            this.label18.Text = "bits";
            // 
            // eExpBits
            // 
            this.eExpBits.Enabled = false;
            this.eExpBits.Location = new System.Drawing.Point(908, 375);
            this.eExpBits.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.eExpBits.Name = "eExpBits";
            this.eExpBits.ReadOnly = true;
            this.eExpBits.Size = new System.Drawing.Size(21, 23);
            this.eExpBits.TabIndex = 31;
            this.eExpBits.TabStop = false;
            this.eExpBits.Text = "52";
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label17.Location = new System.Drawing.Point(841, 380);
            this.label17.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(70, 18);
            this.label17.TabIndex = 30;
            this.label17.Text = "bits ,  exp";
            // 
            // eRealPrecision
            // 
            this.eRealPrecision.Location = new System.Drawing.Point(782, 376);
            this.eRealPrecision.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.eRealPrecision.Name = "eRealPrecision";
            this.eRealPrecision.Size = new System.Drawing.Size(58, 23);
            this.eRealPrecision.TabIndex = 29;
            this.eRealPrecision.Text = "52";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label16.Location = new System.Drawing.Point(704, 381);
            this.label16.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(80, 18);
            this.label16.TabIndex = 28;
            this.label16.Text = ", mantissa ";
            // 
            // cbRealType
            // 
            this.cbRealType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbRealType.FormattingEnabled = true;
            this.cbRealType.Items.AddRange(new object[] {
            "DOUBLE  64bit ~ 15 digits *10^308 , + most functions as native, fast - low exp ra" +
                "nge ",
            "MPFR    96+bit ~  def. digits *10^9digits , +large precision, good exp range, all" +
                " functions native, - slow",
            "QUAD    128bit ~ 18 digits *10^19digits , +huge exp range, - custom, unverified a" +
                "ccuracy"});
            this.cbRealType.Location = new System.Drawing.Point(106, 376);
            this.cbRealType.Name = "cbRealType";
            this.cbRealType.Size = new System.Drawing.Size(591, 23);
            this.cbRealType.TabIndex = 27;
            this.cbRealType.SelectedIndexChanged += new System.EventHandler(this.cbRealType_SelectedIndexChanged);
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label15.Location = new System.Drawing.Point(14, 381);
            this.label15.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(92, 18);
            this.label15.TabIndex = 26;
            this.label15.Text = "Number type";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label13.Location = new System.Drawing.Point(14, 315);
            this.label13.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(361, 18);
            this.label13.TabIndex = 25;
            this.label13.Tag = "bit#0:  recalculate only lines below latest change, bit#1:  constant functions,  " +
    "bit#2: cache function results";
            this.label13.Text = "Disable specific optimizations, based on bitmask here";
            // 
            // eDisableMask
            // 
            this.eDisableMask.Location = new System.Drawing.Point(442, 316);
            this.eDisableMask.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.eDisableMask.Name = "eDisableMask";
            this.eDisableMask.Size = new System.Drawing.Size(103, 23);
            this.eDisableMask.TabIndex = 24;
            this.eDisableMask.Tag = "(1==bit#0):  recalculate only lines below latest change, (2==bit#1):  constant fu" +
    "nctions,  (3==bit#2): cache function results";
            // 
            // cbAutoFocusError
            // 
            this.cbAutoFocusError.AutoSize = true;
            this.cbAutoFocusError.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.cbAutoFocusError.Location = new System.Drawing.Point(18, 211);
            this.cbAutoFocusError.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cbAutoFocusError.Name = "cbAutoFocusError";
            this.cbAutoFocusError.Size = new System.Drawing.Size(802, 22);
            this.cbAutoFocusError.TabIndex = 22;
            this.cbAutoFocusError.Tag = "It will always change cursor position on Error, so it is disabled by default";
            this.cbAutoFocusError.Text = "Automatically mark and position at error line in Notepad panel ( alternative is d" +
    "ouble-click on error in left Results panel )";
            this.cbAutoFocusError.UseVisualStyleBackColor = true;
            // 
            // eFormatSeparator
            // 
            this.eFormatSeparator.Location = new System.Drawing.Point(529, 347);
            this.eFormatSeparator.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.eFormatSeparator.Name = "eFormatSeparator";
            this.eFormatSeparator.Size = new System.Drawing.Size(29, 23);
            this.eFormatSeparator.TabIndex = 21;
            this.eFormatSeparator.Tag = "If result is 12345678.9 and separator is \',\' then result is shown as  12,345,678." +
    "9";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label11.Location = new System.Drawing.Point(370, 347);
            this.label11.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(151, 18);
            this.label11.TabIndex = 20;
            this.label11.Text = ", digit group separator";
            // 
            // eFormatDecimals
            // 
            this.eFormatDecimals.Location = new System.Drawing.Point(322, 347);
            this.eFormatDecimals.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.eFormatDecimals.Name = "eFormatDecimals";
            this.eFormatDecimals.Size = new System.Drawing.Size(40, 23);
            this.eFormatDecimals.TabIndex = 19;
            this.eFormatDecimals.Tag = "Leave empty for all decimals. If result is 12.345678 , then max decimals 2 will m" +
    "ake it: 12.34  (vector values are displayed at max 2 decimals)";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label12.Location = new System.Drawing.Point(14, 346);
            this.label12.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(261, 18);
            this.label12.TabIndex = 18;
            this.label12.Text = "Format result numbers:  max decimals";
            // 
            // eDebugVars
            // 
            this.eDebugVars.Location = new System.Drawing.Point(477, 285);
            this.eDebugVars.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.eDebugVars.Name = "eDebugVars";
            this.eDebugVars.Size = new System.Drawing.Size(481, 23);
            this.eDebugVars.TabIndex = 17;
            this.eDebugVars.Tag = "Variable values are shown in lower-right error pane, whenever one of listed varia" +
    "bles change value. Use \"*\" to show ALL variables.";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label10.Location = new System.Drawing.Point(14, 284);
            this.label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(403, 18);
            this.label10.TabIndex = 16;
            this.label10.Text = "Show debug for variables (comma separated names or \"*\" ):";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label9.Location = new System.Drawing.Point(385, 254);
            this.label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(429, 18);
            this.label9.TabIndex = 15;
            this.label9.Text = "characters entered in new word ( use Ctrl-Space to force show )";
            // 
            // eAutocompleteChars
            // 
            this.eAutocompleteChars.Location = new System.Drawing.Point(340, 255);
            this.eAutocompleteChars.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.eAutocompleteChars.Name = "eAutocompleteChars";
            this.eAutocompleteChars.Size = new System.Drawing.Size(38, 23);
            this.eAutocompleteChars.TabIndex = 14;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label8.Location = new System.Drawing.Point(14, 254);
            this.label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(284, 18);
            this.label8.TabIndex = 13;
            this.label8.Text = "Show notepad Autocomplete popup after  ";
            // 
            // cbDisplayBinHexFloat
            // 
            this.cbDisplayBinHexFloat.AutoSize = true;
            this.cbDisplayBinHexFloat.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.cbDisplayBinHexFloat.Location = new System.Drawing.Point(18, 179);
            this.cbDisplayBinHexFloat.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cbDisplayBinHexFloat.Name = "cbDisplayBinHexFloat";
            this.cbDisplayBinHexFloat.Size = new System.Drawing.Size(467, 22);
            this.cbDisplayBinHexFloat.TabIndex = 11;
            this.cbDisplayBinHexFloat.Tag = "For example, after \'bin\' is used :   5/2 -> 0b10.1 instead of 2.5 , while 6/2 alw" +
    "ays result in 0b10 ";
            this.cbDisplayBinHexFloat.Text = "If Hex or Bin used, display  even non-integer numbers as bin or hex ";
            this.cbDisplayBinHexFloat.UseVisualStyleBackColor = true;
            // 
            // cbNativeExponent
            // 
            this.cbNativeExponent.AutoSize = true;
            this.cbNativeExponent.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.cbNativeExponent.Location = new System.Drawing.Point(18, 147);
            this.cbNativeExponent.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cbNativeExponent.Name = "cbNativeExponent";
            this.cbNativeExponent.Size = new System.Drawing.Size(721, 22);
            this.cbNativeExponent.TabIndex = 10;
            this.cbNativeExponent.Tag = "For example:  0x3XA==3*16^10 , instead of 0x3X10  ;  or for bin  0b101E10 instead" +
    " of 0b101E2";
            this.cbNativeExponent.Text = "Assume native exponent for hex/bin numbers instead of decimal exponent ( for hexa" +
    ", use \'X\" instead of \'E\')";
            this.cbNativeExponent.UseVisualStyleBackColor = true;
            // 
            // cbAllowBuiltinRedefinition
            // 
            this.cbAllowBuiltinRedefinition.AutoSize = true;
            this.cbAllowBuiltinRedefinition.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.cbAllowBuiltinRedefinition.Location = new System.Drawing.Point(18, 114);
            this.cbAllowBuiltinRedefinition.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cbAllowBuiltinRedefinition.Name = "cbAllowBuiltinRedefinition";
            this.cbAllowBuiltinRedefinition.Size = new System.Drawing.Size(285, 22);
            this.cbAllowBuiltinRedefinition.TabIndex = 9;
            this.cbAllowBuiltinRedefinition.Tag = "Only works if general redefinition of functions is also allowed.";
            this.cbAllowBuiltinRedefinition.Text = "Allow redefinition of builtin functions too";
            this.cbAllowBuiltinRedefinition.UseVisualStyleBackColor = true;
            // 
            // cbAllowFunctionRedefinition
            // 
            this.cbAllowFunctionRedefinition.AutoSize = true;
            this.cbAllowFunctionRedefinition.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.cbAllowFunctionRedefinition.Location = new System.Drawing.Point(18, 82);
            this.cbAllowFunctionRedefinition.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cbAllowFunctionRedefinition.Name = "cbAllowFunctionRedefinition";
            this.cbAllowFunctionRedefinition.Size = new System.Drawing.Size(495, 22);
            this.cbAllowFunctionRedefinition.TabIndex = 8;
            this.cbAllowFunctionRedefinition.Tag = "For example :  Sin(x)= sin(x[deg]) ,  so Sin(90)==sin(pi/2) . Or [M]=1000000, whi" +
    "le [m]=1";
            this.cbAllowFunctionRedefinition.Text = "Allow redefinition of functions  ( prevents some constant optimizations )";
            this.cbAllowFunctionRedefinition.UseVisualStyleBackColor = true;
            this.cbAllowFunctionRedefinition.CheckedChanged += new System.EventHandler(this.cbAllowFunctionRedefinition_CheckedChanged);
            // 
            // cbShowExecutionTime
            // 
            this.cbShowExecutionTime.AutoSize = true;
            this.cbShowExecutionTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.cbShowExecutionTime.Location = new System.Drawing.Point(18, 50);
            this.cbShowExecutionTime.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cbShowExecutionTime.Name = "cbShowExecutionTime";
            this.cbShowExecutionTime.Size = new System.Drawing.Size(595, 22);
            this.cbShowExecutionTime.TabIndex = 7;
            this.cbShowExecutionTime.Tag = "display time ( in miliseconds ) next to each execution line result";
            this.cbShowExecutionTime.Text = "Show execution time next to results  ( by default it is shown only when timeout o" +
    "ccurs )";
            this.cbShowExecutionTime.UseVisualStyleBackColor = true;
            // 
            // cbTimeoutDisabled
            // 
            this.cbTimeoutDisabled.AutoSize = true;
            this.cbTimeoutDisabled.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.cbTimeoutDisabled.Location = new System.Drawing.Point(807, 17);
            this.cbTimeoutDisabled.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cbTimeoutDisabled.Name = "cbTimeoutDisabled";
            this.cbTimeoutDisabled.Size = new System.Drawing.Size(130, 22);
            this.cbTimeoutDisabled.TabIndex = 6;
            this.cbTimeoutDisabled.Tag = "Ignore previous timeout values and allow indefinite calculation time.";
            this.cbTimeoutDisabled.Text = "disable timeout.";
            this.cbTimeoutDisabled.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label6.Location = new System.Drawing.Point(694, 18);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(91, 18);
            this.label6.TabIndex = 5;
            this.label6.Text = "seconds, or ";
            // 
            // eTimeoutDoc
            // 
            this.eTimeoutDoc.Location = new System.Drawing.Point(649, 20);
            this.eTimeoutDoc.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.eTimeoutDoc.Name = "eTimeoutDoc";
            this.eTimeoutDoc.Size = new System.Drawing.Size(38, 23);
            this.eTimeoutDoc.TabIndex = 4;
            this.eTimeoutDoc.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.eTimeoutFunc_KeyPress);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label5.Location = new System.Drawing.Point(336, 18);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(274, 18);
            this.label5.TabIndex = 3;
            this.label5.Text = "miliseconds , and for entire document is ";
            // 
            // eTimeoutFunc
            // 
            this.eTimeoutFunc.Location = new System.Drawing.Point(251, 20);
            this.eTimeoutFunc.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.eTimeoutFunc.Name = "eTimeoutFunc";
            this.eTimeoutFunc.Size = new System.Drawing.Size(78, 23);
            this.eTimeoutFunc.TabIndex = 2;
            this.eTimeoutFunc.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.eTimeoutFunc_KeyPress);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label4.Location = new System.Drawing.Point(14, 18);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(197, 18);
            this.label4.TabIndex = 1;
            this.label4.Text = "Timeout for single function is";
            // 
            // appVersion
            // 
            this.appVersion.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.appVersion.Location = new System.Drawing.Point(826, 758);
            this.appVersion.Name = "appVersion";
            this.appVersion.Size = new System.Drawing.Size(172, 21);
            this.appVersion.TabIndex = 4;
            this.appVersion.Text = "v 2.8.xx";
            this.appVersion.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // ConfigForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1013, 800);
            this.Controls.Add(this.appVersion);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "ConfigForm";
            this.Text = "Options";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ConfigForm_FormClosed);
            this.Load += new System.EventHandler(this.ConfigForm_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbSelectFile)).EndInit();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TextBox ePresetFile;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.PictureBox pbSelectFile;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.CheckBox cbTimeoutDisabled;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox eTimeoutDoc;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox eTimeoutFunc;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox cbDisplayBinHexFloat;
        private System.Windows.Forms.CheckBox cbNativeExponent;
        private System.Windows.Forms.CheckBox cbAllowBuiltinRedefinition;
        private System.Windows.Forms.CheckBox cbAllowFunctionRedefinition;
        private System.Windows.Forms.CheckBox cbShowExecutionTime;
        private System.Windows.Forms.ComboBox cbCase;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.CheckBox cbReplaceKnownSymbols;
        private System.Windows.Forms.CheckBox cbAutoLastResult;
        private System.Windows.Forms.CheckBox cbLoadLast;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox eAutocompleteChars;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox eDebugVars;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox eFormatSeparator;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox eFormatDecimals;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.CheckBox cbAutoFocusError;
        private System.Windows.Forms.TextBox eDisableMask;
        private System.Windows.Forms.Label label13;
        private Label appVersion;
        private TextBox eRealPrecision;
        private Label label16;
        private ComboBox cbRealType;
        private Label label15;
        private Label label18;
        private TextBox eExpBits;
        private Label label17;
        private TextBox eFractionSeparator;
        private Label label19;
    }
}