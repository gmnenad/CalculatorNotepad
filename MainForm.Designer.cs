namespace CalculatorNotepad
{
    partial class FormCalculator
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormCalculator));
            this.pbMenu = new System.Windows.Forms.PictureBox();
            this.pbCSpanel = new System.Windows.Forms.PictureBox();
            this.pbHelp = new System.Windows.Forms.PictureBox();
            this.pbClear = new System.Windows.Forms.PictureBox();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.eMsg = new System.Windows.Forms.TextBox();
            this.splitContainerAll = new System.Windows.Forms.SplitContainer();
            this.fbResults = new FastColoredTextBoxNS.FastColoredTextBox();
            this.splitContainerCode = new System.Windows.Forms.SplitContainer();
            this.fbNotepad = new FastColoredTextBoxNS.FastColoredTextBox();
            this.splitContainerCSharp = new System.Windows.Forms.SplitContainer();
            this.fbCSharp = new FastColoredTextBoxNS.FastColoredTextBox();
            this.fbErrors = new FastColoredTextBoxNS.FastColoredTextBox();
            ((System.ComponentModel.ISupportInitialize)(this.pbMenu)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbCSpanel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbHelp)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbClear)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerAll)).BeginInit();
            this.splitContainerAll.Panel1.SuspendLayout();
            this.splitContainerAll.Panel2.SuspendLayout();
            this.splitContainerAll.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.fbResults)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerCode)).BeginInit();
            this.splitContainerCode.Panel1.SuspendLayout();
            this.splitContainerCode.Panel2.SuspendLayout();
            this.splitContainerCode.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.fbNotepad)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerCSharp)).BeginInit();
            this.splitContainerCSharp.Panel1.SuspendLayout();
            this.splitContainerCSharp.Panel2.SuspendLayout();
            this.splitContainerCSharp.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.fbCSharp)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.fbErrors)).BeginInit();
            this.SuspendLayout();
            // 
            // pbMenu
            // 
            this.pbMenu.Image = global::CalculatorNotepad.Properties.Resources.Menu_DarkGreen;
            this.pbMenu.Location = new System.Drawing.Point(15, 6);
            this.pbMenu.Margin = new System.Windows.Forms.Padding(0);
            this.pbMenu.Name = "pbMenu";
            this.pbMenu.Size = new System.Drawing.Size(28, 28);
            this.pbMenu.TabIndex = 8;
            this.pbMenu.TabStop = false;
            this.pbMenu.Click += new System.EventHandler(this.pbMenu_Click);
            // 
            // pbCSpanel
            // 
            this.pbCSpanel.Image = global::CalculatorNotepad.Properties.Resources.csharp_icon;
            this.pbCSpanel.Location = new System.Drawing.Point(54, 6);
            this.pbCSpanel.Margin = new System.Windows.Forms.Padding(0);
            this.pbCSpanel.Name = "pbCSpanel";
            this.pbCSpanel.Size = new System.Drawing.Size(28, 28);
            this.pbCSpanel.TabIndex = 9;
            this.pbCSpanel.TabStop = false;
            this.pbCSpanel.Click += new System.EventHandler(this.pbCSpanel_Click);
            // 
            // pbHelp
            // 
            this.pbHelp.Image = global::CalculatorNotepad.Properties.Resources.Help;
            this.pbHelp.Location = new System.Drawing.Point(96, 6);
            this.pbHelp.Margin = new System.Windows.Forms.Padding(0);
            this.pbHelp.Name = "pbHelp";
            this.pbHelp.Size = new System.Drawing.Size(28, 28);
            this.pbHelp.TabIndex = 10;
            this.pbHelp.TabStop = false;
            this.pbHelp.Click += new System.EventHandler(this.pbHelp_Click);
            // 
            // pbClear
            // 
            this.pbClear.Image = global::CalculatorNotepad.Properties.Resources.Reset_red;
            this.pbClear.Location = new System.Drawing.Point(138, 6);
            this.pbClear.Margin = new System.Windows.Forms.Padding(0);
            this.pbClear.Name = "pbClear";
            this.pbClear.Size = new System.Drawing.Size(28, 28);
            this.pbClear.TabIndex = 11;
            this.pbClear.TabStop = false;
            this.pbClear.Click += new System.EventHandler(this.pbClear_Click);
            // 
            // imageList1
            // 
            this.imageList1.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "Menu_DarkGreen.png");
            this.imageList1.Images.SetKeyName(1, "csharp-icon.png");
            this.imageList1.Images.SetKeyName(2, "Help.png");
            this.imageList1.Images.SetKeyName(3, "Reset_red.png");
            // 
            // eMsg
            // 
            this.eMsg.BackColor = System.Drawing.SystemColors.Control;
            this.eMsg.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.eMsg.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.eMsg.ForeColor = System.Drawing.Color.Red;
            this.eMsg.Location = new System.Drawing.Point(183, 8);
            this.eMsg.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.eMsg.Name = "eMsg";
            this.eMsg.ReadOnly = true;
            this.eMsg.Size = new System.Drawing.Size(1224, 19);
            this.eMsg.TabIndex = 7;
            // 
            // splitContainerAll
            // 
            this.splitContainerAll.Location = new System.Drawing.Point(7, 37);
            this.splitContainerAll.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.splitContainerAll.Name = "splitContainerAll";
            // 
            // splitContainerAll.Panel1
            // 
            this.splitContainerAll.Panel1.Controls.Add(this.fbResults);
            // 
            // splitContainerAll.Panel2
            // 
            this.splitContainerAll.Panel2.Controls.Add(this.splitContainerCode);
            this.splitContainerAll.Size = new System.Drawing.Size(1400, 780);
            this.splitContainerAll.SplitterDistance = 200;
            this.splitContainerAll.SplitterWidth = 5;
            this.splitContainerAll.TabIndex = 5;
            // 
            // fbResults
            // 
            this.fbResults.AutoCompleteBracketsList = new char[] {
        '(',
        ')',
        '{',
        '}',
        '[',
        ']',
        '\"',
        '\"',
        '\'',
        '\''};
            this.fbResults.AutoScrollMinSize = new System.Drawing.Size(2, 20);
            this.fbResults.BackBrush = null;
            this.fbResults.BackColor = System.Drawing.Color.WhiteSmoke;
            this.fbResults.CharHeight = 20;
            this.fbResults.CharWidth = 9;
            this.fbResults.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.fbResults.DisabledColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.fbResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.fbResults.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.fbResults.IsReplaceMode = false;
            this.fbResults.LineInterval = 2;
            this.fbResults.Location = new System.Drawing.Point(0, 0);
            this.fbResults.Margin = new System.Windows.Forms.Padding(0);
            this.fbResults.Name = "fbResults";
            this.fbResults.Paddings = new System.Windows.Forms.Padding(0);
            this.fbResults.ReadOnly = true;
            this.fbResults.SelectionColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(255)))));
            this.fbResults.ServiceColors = ((FastColoredTextBoxNS.ServiceColors)(resources.GetObject("fbResults.ServiceColors")));
            this.fbResults.ShowLineNumbers = false;
            this.fbResults.ShowScrollBars = false;
            this.fbResults.Size = new System.Drawing.Size(200, 780);
            this.fbResults.TabIndex = 1;
            this.fbResults.Zoom = 100;
            this.fbResults.ToolTipNeeded += new System.EventHandler<FastColoredTextBoxNS.ToolTipNeededEventArgs>(this.fbResults_ToolTipNeeded);
            this.fbResults.SizeChanged += new System.EventHandler(this.fbResults_SizeChanged);
            this.fbResults.DoubleClick += new System.EventHandler(this.fbResults_DoubleClick);
            // 
            // splitContainerCode
            // 
            this.splitContainerCode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerCode.Location = new System.Drawing.Point(0, 0);
            this.splitContainerCode.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.splitContainerCode.Name = "splitContainerCode";
            // 
            // splitContainerCode.Panel1
            // 
            this.splitContainerCode.Panel1.Controls.Add(this.fbNotepad);
            // 
            // splitContainerCode.Panel2
            // 
            this.splitContainerCode.Panel2.Controls.Add(this.splitContainerCSharp);
            this.splitContainerCode.Size = new System.Drawing.Size(1195, 780);
            this.splitContainerCode.SplitterDistance = 790;
            this.splitContainerCode.SplitterWidth = 5;
            this.splitContainerCode.TabIndex = 4;
            // 
            // fbNotepad
            // 
            this.fbNotepad.AutoCompleteBracketsList = new char[] {
        '(',
        ')',
        '{',
        '}',
        '[',
        ']',
        '\"',
        '\"',
        '\'',
        '\''};
            this.fbNotepad.AutoScrollMinSize = new System.Drawing.Size(12, 20);
            this.fbNotepad.BackBrush = null;
            this.fbNotepad.CharHeight = 20;
            this.fbNotepad.CharWidth = 9;
            this.fbNotepad.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.fbNotepad.DisabledColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.fbNotepad.Dock = System.Windows.Forms.DockStyle.Fill;
            this.fbNotepad.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.fbNotepad.IsReplaceMode = false;
            this.fbNotepad.LineInterval = 2;
            this.fbNotepad.Location = new System.Drawing.Point(0, 0);
            this.fbNotepad.Margin = new System.Windows.Forms.Padding(0);
            this.fbNotepad.Name = "fbNotepad";
            this.fbNotepad.Paddings = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.fbNotepad.SelectionColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(255)))));
            this.fbNotepad.ServiceColors = ((FastColoredTextBoxNS.ServiceColors)(resources.GetObject("fbNotepad.ServiceColors")));
            this.fbNotepad.ShowLineNumbers = false;
            this.fbNotepad.Size = new System.Drawing.Size(790, 780);
            this.fbNotepad.TabIndex = 0;
            this.fbNotepad.Zoom = 100;
            this.fbNotepad.ToolTipNeeded += new System.EventHandler<FastColoredTextBoxNS.ToolTipNeededEventArgs>(this.fbNotepad_ToolTipNeeded);
            this.fbNotepad.TextChanged += new System.EventHandler<FastColoredTextBoxNS.TextChangedEventArgs>(this.fbNotepad_TextChanged);
            this.fbNotepad.SelectionChanged += new System.EventHandler(this.fbNotepad_SelectionChanged);
            this.fbNotepad.SelectionChangedDelayed += new System.EventHandler(this.fbNotepad_SelectionChangedDelayed);
            this.fbNotepad.ScrollbarsUpdated += new System.EventHandler(this.fbNotepad_ScrollbarsUpdated);
            // 
            // splitContainerCSharp
            // 
            this.splitContainerCSharp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerCSharp.Location = new System.Drawing.Point(0, 0);
            this.splitContainerCSharp.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.splitContainerCSharp.Name = "splitContainerCSharp";
            this.splitContainerCSharp.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainerCSharp.Panel1
            // 
            this.splitContainerCSharp.Panel1.Controls.Add(this.fbCSharp);
            // 
            // splitContainerCSharp.Panel2
            // 
            this.splitContainerCSharp.Panel2.Controls.Add(this.fbErrors);
            this.splitContainerCSharp.Size = new System.Drawing.Size(400, 780);
            this.splitContainerCSharp.SplitterDistance = 619;
            this.splitContainerCSharp.SplitterWidth = 5;
            this.splitContainerCSharp.TabIndex = 5;
            // 
            // fbCSharp
            // 
            this.fbCSharp.AutoCompleteBracketsList = new char[] {
        '(',
        ')',
        '{',
        '}',
        '[',
        ']',
        '\"',
        '\"',
        '\'',
        '\''};
            this.fbCSharp.AutoScrollMinSize = new System.Drawing.Size(29, 18);
            this.fbCSharp.BackBrush = null;
            this.fbCSharp.CharHeight = 18;
            this.fbCSharp.CharWidth = 9;
            this.fbCSharp.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.fbCSharp.DisabledColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.fbCSharp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.fbCSharp.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.fbCSharp.IsReplaceMode = false;
            this.fbCSharp.Location = new System.Drawing.Point(0, 0);
            this.fbCSharp.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.fbCSharp.Name = "fbCSharp";
            this.fbCSharp.Paddings = new System.Windows.Forms.Padding(0);
            this.fbCSharp.SelectionColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(255)))));
            this.fbCSharp.ServiceColors = ((FastColoredTextBoxNS.ServiceColors)(resources.GetObject("fbCSharp.ServiceColors")));
            this.fbCSharp.Size = new System.Drawing.Size(400, 619);
            this.fbCSharp.TabIndex = 1;
            this.fbCSharp.Zoom = 100;
            this.fbCSharp.Leave += new System.EventHandler(this.fbCSharp_Leave);
            // 
            // fbErrors
            // 
            this.fbErrors.AutoCompleteBracketsList = new char[] {
        '(',
        ')',
        '{',
        '}',
        '[',
        ']',
        '\"',
        '\"',
        '\'',
        '\''};
            this.fbErrors.AutoScrollMinSize = new System.Drawing.Size(2, 18);
            this.fbErrors.BackBrush = null;
            this.fbErrors.BackColor = System.Drawing.Color.WhiteSmoke;
            this.fbErrors.CharHeight = 18;
            this.fbErrors.CharWidth = 9;
            this.fbErrors.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.fbErrors.DisabledColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.fbErrors.Dock = System.Windows.Forms.DockStyle.Fill;
            this.fbErrors.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.fbErrors.IsReplaceMode = false;
            this.fbErrors.Location = new System.Drawing.Point(0, 0);
            this.fbErrors.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
            this.fbErrors.Name = "fbErrors";
            this.fbErrors.Paddings = new System.Windows.Forms.Padding(0);
            this.fbErrors.ReadOnly = true;
            this.fbErrors.SelectionColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(255)))));
            this.fbErrors.ServiceColors = ((FastColoredTextBoxNS.ServiceColors)(resources.GetObject("fbErrors.ServiceColors")));
            this.fbErrors.ShowLineNumbers = false;
            this.fbErrors.Size = new System.Drawing.Size(400, 156);
            this.fbErrors.TabIndex = 2;
            this.fbErrors.Zoom = 100;
            this.fbErrors.DoubleClick += new System.EventHandler(this.fbErrors_DoubleClick);
            // 
            // FormCalculator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLight;
            this.ClientSize = new System.Drawing.Size(1412, 673);
            this.Controls.Add(this.splitContainerAll);
            this.Controls.Add(this.eMsg);
            this.Controls.Add(this.pbClear);
            this.Controls.Add(this.pbHelp);
            this.Controls.Add(this.pbCSpanel);
            this.Controls.Add(this.pbMenu);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "FormCalculator";
            this.Text = "Calculator Notepad";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormCalculator_FormClosing);
            this.Load += new System.EventHandler(this.FormCalculator_Load);
            this.ResizeEnd += new System.EventHandler(this.Form1_ResizeEnd);
            this.SizeChanged += new System.EventHandler(this.FormCalculator_SizeChanged);
            ((System.ComponentModel.ISupportInitialize)(this.pbMenu)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbCSpanel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbHelp)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbClear)).EndInit();
            this.splitContainerAll.Panel1.ResumeLayout(false);
            this.splitContainerAll.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerAll)).EndInit();
            this.splitContainerAll.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.fbResults)).EndInit();
            this.splitContainerCode.Panel1.ResumeLayout(false);
            this.splitContainerCode.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerCode)).EndInit();
            this.splitContainerCode.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.fbNotepad)).EndInit();
            this.splitContainerCSharp.Panel1.ResumeLayout(false);
            this.splitContainerCSharp.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerCSharp)).EndInit();
            this.splitContainerCSharp.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.fbCSharp)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.fbErrors)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private PictureBox pbMenu;
        private PictureBox pbCSpanel;
        private PictureBox pbHelp;
        private PictureBox pbClear;
        private ImageList imageList1;
        private TextBox eMsg;
        private SplitContainer splitContainerAll;
        private FastColoredTextBoxNS.FastColoredTextBox fbResults;
        private SplitContainer splitContainerCode;
        private FastColoredTextBoxNS.FastColoredTextBox fbNotepad;
        private SplitContainer splitContainerCSharp;
        private FastColoredTextBoxNS.FastColoredTextBox fbCSharp;
        private FastColoredTextBoxNS.FastColoredTextBox fbErrors;
    }
}