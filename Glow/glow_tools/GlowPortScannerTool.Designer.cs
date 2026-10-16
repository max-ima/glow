namespace Glow.glow_tools
{
    partial class GlowPortScannerTool
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle10 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle11 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle12 = new System.Windows.Forms.DataGridViewCellStyle();
            this.PST_DataTable = new System.Windows.Forms.DataGridView();
            this.PST_BottomPanel = new System.Windows.Forms.TableLayoutPanel();
            this.PST_OpenPortsBtn = new Glow.TSCustomButton();
            this.PST_RemotePortBtn = new Glow.TSCustomButton();
            this.PST_ExportBtn = new Glow.TSCustomButton();
            this.PST_AutoRefreshBtn = new Glow.TSCustomButton();
            this.PST_FilterPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.PST_SortMode = new Glow.TSCustomCheckBox();
            this.PST_OpenOnly = new Glow.TSCustomCheckBox();
            this.PST_TotalCount = new System.Windows.Forms.Label();
            this.PST_SearchLabel = new System.Windows.Forms.Label();
            this.PST_TextBox = new System.Windows.Forms.TextBox();
            this.PST_TotalCount_V = new System.Windows.Forms.Label();
            this.PST_RemoteHostLabel = new System.Windows.Forms.Label();
            this.PST_RemoteHostTextBox = new System.Windows.Forms.TextBox();
            this.PST_RemotePortsLabel = new System.Windows.Forms.Label();
            this.PST_RemotePortsTextBox = new System.Windows.Forms.TextBox();
            this.PST_RemoteTimeoutLabel = new System.Windows.Forms.Label();
            this.PST_RemoteTimeoutTextBox = new System.Windows.Forms.TextBox();
            this.PST_RemotePanel = new System.Windows.Forms.FlowLayoutPanel();
            this.PST_IpInfoBtn = new Glow.TSCustomButton();
            this.PST_ToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.PST_TextBoxClearBtn = new Glow.TSCustomButton();
            ((System.ComponentModel.ISupportInitialize)(this.PST_DataTable)).BeginInit();
            this.PST_BottomPanel.SuspendLayout();
            this.PST_FilterPanel.SuspendLayout();
            this.PST_RemotePanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // PST_DataTable
            // 
            this.PST_DataTable.AllowUserToAddRows = false;
            this.PST_DataTable.AllowUserToDeleteRows = false;
            this.PST_DataTable.AllowUserToResizeRows = false;
            dataGridViewCellStyle9.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(237)))), ((int)(((byte)(237)))), ((int)(((byte)(237)))));
            this.PST_DataTable.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle9;
            this.PST_DataTable.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PST_DataTable.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.PST_DataTable.BackgroundColor = System.Drawing.Color.White;
            this.PST_DataTable.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.PST_DataTable.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle10.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle10.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(4)))), ((int)(((byte)(87)))), ((int)(((byte)(160)))));
            dataGridViewCellStyle10.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            dataGridViewCellStyle10.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle10.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
            dataGridViewCellStyle10.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(4)))), ((int)(((byte)(87)))), ((int)(((byte)(160)))));
            dataGridViewCellStyle10.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle10.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.PST_DataTable.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle10;
            this.PST_DataTable.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.PST_DataTable.Cursor = System.Windows.Forms.Cursors.Hand;
            dataGridViewCellStyle11.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle11.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle11.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            dataGridViewCellStyle11.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle11.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(4)))), ((int)(((byte)(87)))), ((int)(((byte)(160)))));
            dataGridViewCellStyle11.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle11.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.PST_DataTable.DefaultCellStyle = dataGridViewCellStyle11;
            this.PST_DataTable.EnableHeadersVisualStyles = false;
            this.PST_DataTable.GridColor = System.Drawing.Color.Gray;
            this.PST_DataTable.Location = new System.Drawing.Point(13, 128);
            this.PST_DataTable.MultiSelect = false;
            this.PST_DataTable.Name = "PST_DataTable";
            this.PST_DataTable.ReadOnly = true;
            dataGridViewCellStyle12.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle12.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle12.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            dataGridViewCellStyle12.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle12.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(4)))), ((int)(((byte)(87)))), ((int)(((byte)(160)))));
            dataGridViewCellStyle12.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle12.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.PST_DataTable.RowHeadersDefaultCellStyle = dataGridViewCellStyle12;
            this.PST_DataTable.RowHeadersVisible = false;
            this.PST_DataTable.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.PST_DataTable.Size = new System.Drawing.Size(982, 373);
            this.PST_DataTable.TabIndex = 4;
            // 
            // PST_BottomPanel
            // 
            this.PST_BottomPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PST_BottomPanel.ColumnCount = 2;
            this.PST_BottomPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.PST_BottomPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.PST_BottomPanel.Controls.Add(this.PST_OpenPortsBtn, 0, 0);
            this.PST_BottomPanel.Controls.Add(this.PST_RemotePortBtn, 1, 0);
            this.PST_BottomPanel.Controls.Add(this.PST_ExportBtn, 0, 1);
            this.PST_BottomPanel.Controls.Add(this.PST_AutoRefreshBtn, 1, 1);
            this.PST_BottomPanel.Location = new System.Drawing.Point(13, 511);
            this.PST_BottomPanel.Name = "PST_BottomPanel";
            this.PST_BottomPanel.RowCount = 2;
            this.PST_BottomPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.PST_BottomPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.PST_BottomPanel.Size = new System.Drawing.Size(982, 80);
            this.PST_BottomPanel.TabIndex = 5;
            // 
            // PST_OpenPortsBtn
            // 
            this.PST_OpenPortsBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(95)))), ((int)(((byte)(146)))));
            this.PST_OpenPortsBtn.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(95)))), ((int)(((byte)(146)))));
            this.PST_OpenPortsBtn.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.PST_OpenPortsBtn.BorderColor = System.Drawing.Color.DodgerBlue;
            this.PST_OpenPortsBtn.BorderRadius = 10;
            this.PST_OpenPortsBtn.BorderSize = 0;
            this.PST_OpenPortsBtn.Cursor = System.Windows.Forms.Cursors.Hand;
            this.PST_OpenPortsBtn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PST_OpenPortsBtn.FlatAppearance.BorderSize = 0;
            this.PST_OpenPortsBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.PST_OpenPortsBtn.Font = new System.Drawing.Font("Segoe UI Semibold", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.PST_OpenPortsBtn.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.PST_OpenPortsBtn.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.PST_OpenPortsBtn.Location = new System.Drawing.Point(0, 2);
            this.PST_OpenPortsBtn.Margin = new System.Windows.Forms.Padding(0, 2, 5, 2);
            this.PST_OpenPortsBtn.Name = "PST_OpenPortsBtn";
            this.PST_OpenPortsBtn.Size = new System.Drawing.Size(486, 36);
            this.PST_OpenPortsBtn.TabIndex = 0;
            this.PST_OpenPortsBtn.Text = "Yerel Port Tarama";
            this.PST_OpenPortsBtn.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.PST_OpenPortsBtn.TextColor = System.Drawing.Color.WhiteSmoke;
            this.PST_OpenPortsBtn.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.PST_OpenPortsBtn.UseVisualStyleBackColor = false;
            // 
            // PST_RemotePortBtn
            // 
            this.PST_RemotePortBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(95)))), ((int)(((byte)(146)))));
            this.PST_RemotePortBtn.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(95)))), ((int)(((byte)(146)))));
            this.PST_RemotePortBtn.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.PST_RemotePortBtn.BorderColor = System.Drawing.Color.DodgerBlue;
            this.PST_RemotePortBtn.BorderRadius = 10;
            this.PST_RemotePortBtn.BorderSize = 0;
            this.PST_RemotePortBtn.Cursor = System.Windows.Forms.Cursors.Hand;
            this.PST_RemotePortBtn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PST_RemotePortBtn.FlatAppearance.BorderSize = 0;
            this.PST_RemotePortBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.PST_RemotePortBtn.Font = new System.Drawing.Font("Segoe UI Semibold", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.PST_RemotePortBtn.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.PST_RemotePortBtn.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.PST_RemotePortBtn.Location = new System.Drawing.Point(496, 2);
            this.PST_RemotePortBtn.Margin = new System.Windows.Forms.Padding(5, 2, 0, 2);
            this.PST_RemotePortBtn.Name = "PST_RemotePortBtn";
            this.PST_RemotePortBtn.Size = new System.Drawing.Size(486, 36);
            this.PST_RemotePortBtn.TabIndex = 1;
            this.PST_RemotePortBtn.Text = "Uzak Port Tarama";
            this.PST_RemotePortBtn.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.PST_RemotePortBtn.TextColor = System.Drawing.Color.WhiteSmoke;
            this.PST_RemotePortBtn.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.PST_RemotePortBtn.UseVisualStyleBackColor = false;
            // 
            // PST_ExportBtn
            // 
            this.PST_ExportBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(95)))), ((int)(((byte)(146)))));
            this.PST_ExportBtn.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(95)))), ((int)(((byte)(146)))));
            this.PST_ExportBtn.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.PST_ExportBtn.BorderColor = System.Drawing.Color.DodgerBlue;
            this.PST_ExportBtn.BorderRadius = 10;
            this.PST_ExportBtn.BorderSize = 0;
            this.PST_ExportBtn.Cursor = System.Windows.Forms.Cursors.Hand;
            this.PST_ExportBtn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PST_ExportBtn.FlatAppearance.BorderSize = 0;
            this.PST_ExportBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.PST_ExportBtn.Font = new System.Drawing.Font("Segoe UI Semibold", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.PST_ExportBtn.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.PST_ExportBtn.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.PST_ExportBtn.Location = new System.Drawing.Point(0, 42);
            this.PST_ExportBtn.Margin = new System.Windows.Forms.Padding(0, 2, 5, 2);
            this.PST_ExportBtn.Name = "PST_ExportBtn";
            this.PST_ExportBtn.Size = new System.Drawing.Size(486, 36);
            this.PST_ExportBtn.TabIndex = 2;
            this.PST_ExportBtn.Text = "Dışa Aktar";
            this.PST_ExportBtn.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.PST_ExportBtn.TextColor = System.Drawing.Color.WhiteSmoke;
            this.PST_ExportBtn.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.PST_ExportBtn.UseVisualStyleBackColor = false;
            // 
            // PST_AutoRefreshBtn
            // 
            this.PST_AutoRefreshBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(95)))), ((int)(((byte)(146)))));
            this.PST_AutoRefreshBtn.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(95)))), ((int)(((byte)(146)))));
            this.PST_AutoRefreshBtn.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.PST_AutoRefreshBtn.BorderColor = System.Drawing.Color.DodgerBlue;
            this.PST_AutoRefreshBtn.BorderRadius = 10;
            this.PST_AutoRefreshBtn.BorderSize = 0;
            this.PST_AutoRefreshBtn.Cursor = System.Windows.Forms.Cursors.Hand;
            this.PST_AutoRefreshBtn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PST_AutoRefreshBtn.FlatAppearance.BorderSize = 0;
            this.PST_AutoRefreshBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.PST_AutoRefreshBtn.Font = new System.Drawing.Font("Segoe UI Semibold", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.PST_AutoRefreshBtn.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.PST_AutoRefreshBtn.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.PST_AutoRefreshBtn.Location = new System.Drawing.Point(496, 42);
            this.PST_AutoRefreshBtn.Margin = new System.Windows.Forms.Padding(5, 2, 0, 2);
            this.PST_AutoRefreshBtn.Name = "PST_AutoRefreshBtn";
            this.PST_AutoRefreshBtn.Size = new System.Drawing.Size(486, 36);
            this.PST_AutoRefreshBtn.TabIndex = 3;
            this.PST_AutoRefreshBtn.Text = "Otomatik Yenilemeyi Durdur";
            this.PST_AutoRefreshBtn.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.PST_AutoRefreshBtn.TextColor = System.Drawing.Color.WhiteSmoke;
            this.PST_AutoRefreshBtn.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.PST_AutoRefreshBtn.UseVisualStyleBackColor = false;
            // 
            // PST_FilterPanel
            // 
            this.PST_FilterPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PST_FilterPanel.Controls.Add(this.PST_SortMode);
            this.PST_FilterPanel.Controls.Add(this.PST_OpenOnly);
            this.PST_FilterPanel.Location = new System.Drawing.Point(13, 62);
            this.PST_FilterPanel.Name = "PST_FilterPanel";
            this.PST_FilterPanel.Size = new System.Drawing.Size(590, 22);
            this.PST_FilterPanel.TabIndex = 2;
            this.PST_FilterPanel.WrapContents = false;
            // 
            // PST_SortMode
            // 
            this.PST_SortMode.AutoSize = true;
            this.PST_SortMode.BorderRadius = 2F;
            this.PST_SortMode.BorderThickness = 1F;
            this.PST_SortMode.CheckedColor = System.Drawing.Color.DodgerBlue;
            this.PST_SortMode.CheckMarkColor = System.Drawing.Color.White;
            this.PST_SortMode.Cursor = System.Windows.Forms.Cursors.Hand;
            this.PST_SortMode.DrawUncheckedFill = false;
            this.PST_SortMode.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.PST_SortMode.Location = new System.Drawing.Point(0, 0);
            this.PST_SortMode.Margin = new System.Windows.Forms.Padding(0, 0, 16, 0);
            this.PST_SortMode.MaxBorderRadius = 8F;
            this.PST_SortMode.MaxBorderThickness = 4F;
            this.PST_SortMode.Name = "PST_SortMode";
            this.PST_SortMode.Size = new System.Drawing.Size(113, 21);
            this.PST_SortMode.TabIndex = 0;
            this.PST_SortMode.Text = "Tersten sırala";
            this.PST_SortMode.UncheckedBackColor = System.Drawing.Color.Transparent;
            this.PST_SortMode.UncheckedBorderColor = System.Drawing.Color.Gray;
            this.PST_SortMode.UseVisualStyleBackColor = true;
            // 
            // PST_OpenOnly
            // 
            this.PST_OpenOnly.AutoSize = true;
            this.PST_OpenOnly.BorderRadius = 2F;
            this.PST_OpenOnly.BorderThickness = 1F;
            this.PST_OpenOnly.CheckedColor = System.Drawing.Color.DodgerBlue;
            this.PST_OpenOnly.CheckMarkColor = System.Drawing.Color.White;
            this.PST_OpenOnly.Cursor = System.Windows.Forms.Cursors.Hand;
            this.PST_OpenOnly.DrawUncheckedFill = false;
            this.PST_OpenOnly.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.PST_OpenOnly.Location = new System.Drawing.Point(129, 0);
            this.PST_OpenOnly.Margin = new System.Windows.Forms.Padding(0);
            this.PST_OpenOnly.MaxBorderRadius = 8F;
            this.PST_OpenOnly.MaxBorderThickness = 4F;
            this.PST_OpenOnly.Name = "PST_OpenOnly";
            this.PST_OpenOnly.Size = new System.Drawing.Size(148, 21);
            this.PST_OpenOnly.TabIndex = 1;
            this.PST_OpenOnly.Text = "Sadece açık portlar";
            this.PST_OpenOnly.UncheckedBackColor = System.Drawing.Color.Transparent;
            this.PST_OpenOnly.UncheckedBorderColor = System.Drawing.Color.Gray;
            this.PST_OpenOnly.UseVisualStyleBackColor = true;
            // 
            // PST_TotalCount
            // 
            this.PST_TotalCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.PST_TotalCount.BackColor = System.Drawing.Color.Transparent;
            this.PST_TotalCount.Font = new System.Drawing.Font("Segoe UI Semibold", 10.5F, System.Drawing.FontStyle.Bold);
            this.PST_TotalCount.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.PST_TotalCount.Location = new System.Drawing.Point(691, 21);
            this.PST_TotalCount.Name = "PST_TotalCount";
            this.PST_TotalCount.Size = new System.Drawing.Size(300, 25);
            this.PST_TotalCount.TabIndex = 15;
            this.PST_TotalCount.Text = "Aktif Port Sayısı:";
            this.PST_TotalCount.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // PST_SearchLabel
            // 
            this.PST_SearchLabel.AutoSize = true;
            this.PST_SearchLabel.BackColor = System.Drawing.Color.Transparent;
            this.PST_SearchLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.PST_SearchLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.PST_SearchLabel.Location = new System.Drawing.Point(10, 10);
            this.PST_SearchLabel.Name = "PST_SearchLabel";
            this.PST_SearchLabel.Size = new System.Drawing.Size(33, 19);
            this.PST_SearchLabel.TabIndex = 11;
            this.PST_SearchLabel.Text = "Ara:";
            // 
            // PST_TextBox
            // 
            this.PST_TextBox.BackColor = System.Drawing.Color.WhiteSmoke;
            this.PST_TextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.PST_TextBox.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold);
            this.PST_TextBox.ForeColor = System.Drawing.Color.Black;
            this.PST_TextBox.Location = new System.Drawing.Point(13, 32);
            this.PST_TextBox.Margin = new System.Windows.Forms.Padding(3, 3, 0, 3);
            this.PST_TextBox.MaxLength = 256;
            this.PST_TextBox.Name = "PST_TextBox";
            this.PST_TextBox.Size = new System.Drawing.Size(250, 25);
            this.PST_TextBox.TabIndex = 0;
            // 
            // PST_TotalCount_V
            // 
            this.PST_TotalCount_V.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.PST_TotalCount_V.BackColor = System.Drawing.Color.Transparent;
            this.PST_TotalCount_V.Font = new System.Drawing.Font("Segoe UI Semibold", 10.5F, System.Drawing.FontStyle.Bold);
            this.PST_TotalCount_V.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(95)))), ((int)(((byte)(146)))));
            this.PST_TotalCount_V.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.PST_TotalCount_V.Location = new System.Drawing.Point(741, 44);
            this.PST_TotalCount_V.Name = "PST_TotalCount_V";
            this.PST_TotalCount_V.Size = new System.Drawing.Size(250, 18);
            this.PST_TotalCount_V.TabIndex = 16;
            this.PST_TotalCount_V.Text = "N/A";
            this.PST_TotalCount_V.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // PST_RemoteHostLabel
            // 
            this.PST_RemoteHostLabel.AutoSize = true;
            this.PST_RemoteHostLabel.BackColor = System.Drawing.Color.Transparent;
            this.PST_RemoteHostLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold);
            this.PST_RemoteHostLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.PST_RemoteHostLabel.Location = new System.Drawing.Point(0, 5);
            this.PST_RemoteHostLabel.Margin = new System.Windows.Forms.Padding(0, 5, 4, 0);
            this.PST_RemoteHostLabel.Name = "PST_RemoteHostLabel";
            this.PST_RemoteHostLabel.Size = new System.Drawing.Size(48, 17);
            this.PST_RemoteHostLabel.TabIndex = 17;
            this.PST_RemoteHostLabel.Text = "Hedef:";
            // 
            // PST_RemoteHostTextBox
            // 
            this.PST_RemoteHostTextBox.BackColor = System.Drawing.Color.WhiteSmoke;
            this.PST_RemoteHostTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.PST_RemoteHostTextBox.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold);
            this.PST_RemoteHostTextBox.ForeColor = System.Drawing.Color.Black;
            this.PST_RemoteHostTextBox.Location = new System.Drawing.Point(52, 2);
            this.PST_RemoteHostTextBox.Margin = new System.Windows.Forms.Padding(0, 2, 16, 0);
            this.PST_RemoteHostTextBox.MaxLength = 255;
            this.PST_RemoteHostTextBox.Name = "PST_RemoteHostTextBox";
            this.PST_RemoteHostTextBox.Size = new System.Drawing.Size(170, 25);
            this.PST_RemoteHostTextBox.TabIndex = 0;
            this.PST_RemoteHostTextBox.Text = "127.0.0.1";
            // 
            // PST_RemotePortsLabel
            // 
            this.PST_RemotePortsLabel.AutoSize = true;
            this.PST_RemotePortsLabel.BackColor = System.Drawing.Color.Transparent;
            this.PST_RemotePortsLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold);
            this.PST_RemotePortsLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.PST_RemotePortsLabel.Location = new System.Drawing.Point(238, 5);
            this.PST_RemotePortsLabel.Margin = new System.Windows.Forms.Padding(0, 5, 4, 0);
            this.PST_RemotePortsLabel.Name = "PST_RemotePortsLabel";
            this.PST_RemotePortsLabel.Size = new System.Drawing.Size(53, 17);
            this.PST_RemotePortsLabel.TabIndex = 18;
            this.PST_RemotePortsLabel.Text = "Portlar:";
            // 
            // PST_RemotePortsTextBox
            // 
            this.PST_RemotePortsTextBox.BackColor = System.Drawing.Color.WhiteSmoke;
            this.PST_RemotePortsTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.PST_RemotePortsTextBox.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold);
            this.PST_RemotePortsTextBox.ForeColor = System.Drawing.Color.Black;
            this.PST_RemotePortsTextBox.Location = new System.Drawing.Point(295, 2);
            this.PST_RemotePortsTextBox.Margin = new System.Windows.Forms.Padding(0, 2, 16, 0);
            this.PST_RemotePortsTextBox.MaxLength = 256;
            this.PST_RemotePortsTextBox.Name = "PST_RemotePortsTextBox";
            this.PST_RemotePortsTextBox.Size = new System.Drawing.Size(150, 25);
            this.PST_RemotePortsTextBox.TabIndex = 1;
            this.PST_RemotePortsTextBox.Text = "1-1024";
            // 
            // PST_RemoteTimeoutLabel
            // 
            this.PST_RemoteTimeoutLabel.AutoSize = true;
            this.PST_RemoteTimeoutLabel.BackColor = System.Drawing.Color.Transparent;
            this.PST_RemoteTimeoutLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold);
            this.PST_RemoteTimeoutLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.PST_RemoteTimeoutLabel.Location = new System.Drawing.Point(461, 5);
            this.PST_RemoteTimeoutLabel.Margin = new System.Windows.Forms.Padding(0, 5, 4, 0);
            this.PST_RemoteTimeoutLabel.Name = "PST_RemoteTimeoutLabel";
            this.PST_RemoteTimeoutLabel.Size = new System.Drawing.Size(120, 17);
            this.PST_RemoteTimeoutLabel.TabIndex = 19;
            this.PST_RemoteTimeoutLabel.Text = "Zaman Aşımı (ms):";
            // 
            // PST_RemoteTimeoutTextBox
            // 
            this.PST_RemoteTimeoutTextBox.BackColor = System.Drawing.Color.WhiteSmoke;
            this.PST_RemoteTimeoutTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.PST_RemoteTimeoutTextBox.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold);
            this.PST_RemoteTimeoutTextBox.ForeColor = System.Drawing.Color.Black;
            this.PST_RemoteTimeoutTextBox.Location = new System.Drawing.Point(585, 2);
            this.PST_RemoteTimeoutTextBox.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
            this.PST_RemoteTimeoutTextBox.MaxLength = 6;
            this.PST_RemoteTimeoutTextBox.Name = "PST_RemoteTimeoutTextBox";
            this.PST_RemoteTimeoutTextBox.Size = new System.Drawing.Size(70, 25);
            this.PST_RemoteTimeoutTextBox.TabIndex = 2;
            this.PST_RemoteTimeoutTextBox.Text = "500";
            // 
            // PST_RemotePanel
            // 
            this.PST_RemotePanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PST_RemotePanel.Controls.Add(this.PST_RemoteHostLabel);
            this.PST_RemotePanel.Controls.Add(this.PST_RemoteHostTextBox);
            this.PST_RemotePanel.Controls.Add(this.PST_RemotePortsLabel);
            this.PST_RemotePanel.Controls.Add(this.PST_RemotePortsTextBox);
            this.PST_RemotePanel.Controls.Add(this.PST_RemoteTimeoutLabel);
            this.PST_RemotePanel.Controls.Add(this.PST_RemoteTimeoutTextBox);
            this.PST_RemotePanel.Controls.Add(this.PST_IpInfoBtn);
            this.PST_RemotePanel.Location = new System.Drawing.Point(13, 90);
            this.PST_RemotePanel.Name = "PST_RemotePanel";
            this.PST_RemotePanel.Size = new System.Drawing.Size(982, 32);
            this.PST_RemotePanel.TabIndex = 3;
            this.PST_RemotePanel.WrapContents = false;
            // 
            // PST_IpInfoBtn
            // 
            this.PST_IpInfoBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(95)))), ((int)(((byte)(146)))));
            this.PST_IpInfoBtn.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(95)))), ((int)(((byte)(146)))));
            this.PST_IpInfoBtn.BorderColor = System.Drawing.Color.DodgerBlue;
            this.PST_IpInfoBtn.BorderRadius = 7;
            this.PST_IpInfoBtn.BorderSize = 0;
            this.PST_IpInfoBtn.Cursor = System.Windows.Forms.Cursors.Hand;
            this.PST_IpInfoBtn.Enabled = false;
            this.PST_IpInfoBtn.FlatAppearance.BorderSize = 0;
            this.PST_IpInfoBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.PST_IpInfoBtn.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold);
            this.PST_IpInfoBtn.ForeColor = System.Drawing.Color.White;
            this.PST_IpInfoBtn.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.PST_IpInfoBtn.Location = new System.Drawing.Point(671, 2);
            this.PST_IpInfoBtn.Margin = new System.Windows.Forms.Padding(16, 2, 0, 0);
            this.PST_IpInfoBtn.Name = "PST_IpInfoBtn";
            this.PST_IpInfoBtn.Size = new System.Drawing.Size(150, 25);
            this.PST_IpInfoBtn.TabIndex = 3;
            this.PST_IpInfoBtn.Text = "IP Bilgisi";
            this.PST_IpInfoBtn.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.PST_IpInfoBtn.TextColor = System.Drawing.Color.White;
            this.PST_IpInfoBtn.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.PST_IpInfoBtn.UseVisualStyleBackColor = false;
            // 
            // PST_ToolTip
            // 
            this.PST_ToolTip.OwnerDraw = true;
            this.PST_ToolTip.Draw += new System.Windows.Forms.DrawToolTipEventHandler(this.PST_ToolTip_Draw);
            // 
            // PST_TextBoxClearBtn
            // 
            this.PST_TextBoxClearBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(95)))), ((int)(((byte)(146)))));
            this.PST_TextBoxClearBtn.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(95)))), ((int)(((byte)(146)))));
            this.PST_TextBoxClearBtn.BorderColor = System.Drawing.Color.DodgerBlue;
            this.PST_TextBoxClearBtn.BorderRadius = 3;
            this.PST_TextBoxClearBtn.BorderSize = 0;
            this.PST_TextBoxClearBtn.Cursor = System.Windows.Forms.Cursors.Hand;
            this.PST_TextBoxClearBtn.Enabled = false;
            this.PST_TextBoxClearBtn.FlatAppearance.BorderSize = 0;
            this.PST_TextBoxClearBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.PST_TextBoxClearBtn.Font = new System.Drawing.Font("Segoe UI Semibold", 10.5F, System.Drawing.FontStyle.Bold);
            this.PST_TextBoxClearBtn.ForeColor = System.Drawing.Color.White;
            this.PST_TextBoxClearBtn.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.PST_TextBoxClearBtn.Location = new System.Drawing.Point(266, 31);
            this.PST_TextBoxClearBtn.Name = "PST_TextBoxClearBtn";
            this.PST_TextBoxClearBtn.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.PST_TextBoxClearBtn.Size = new System.Drawing.Size(27, 27);
            this.PST_TextBoxClearBtn.TabIndex = 1;
            this.PST_TextBoxClearBtn.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.PST_TextBoxClearBtn.TextColor = System.Drawing.Color.White;
            this.PST_TextBoxClearBtn.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.PST_TextBoxClearBtn.UseVisualStyleBackColor = false;
            // 
            // GlowPortScannerTool
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(1008, 601);
            this.Controls.Add(this.PST_TextBoxClearBtn);
            this.Controls.Add(this.PST_FilterPanel);
            this.Controls.Add(this.PST_TotalCount);
            this.Controls.Add(this.PST_SearchLabel);
            this.Controls.Add(this.PST_TextBox);
            this.Controls.Add(this.PST_TotalCount_V);
            this.Controls.Add(this.PST_RemotePanel);
            this.Controls.Add(this.PST_BottomPanel);
            this.Controls.Add(this.PST_DataTable);
            this.DoubleBuffered = true;
            this.Icon = global::Glow.Properties.Resources.GlowLogo;
            this.MinimumSize = new System.Drawing.Size(1024, 640);
            this.Name = "GlowPortScannerTool";
            this.Padding = new System.Windows.Forms.Padding(10);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "GlowPortScannerTool";
            this.Load += new System.EventHandler(this.GlowPortScannerTool_Load);
            ((System.ComponentModel.ISupportInitialize)(this.PST_DataTable)).EndInit();
            this.PST_BottomPanel.ResumeLayout(false);
            this.PST_FilterPanel.ResumeLayout(false);
            this.PST_FilterPanel.PerformLayout();
            this.PST_RemotePanel.ResumeLayout(false);
            this.PST_RemotePanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView PST_DataTable;
        private TSCustomButton PST_OpenPortsBtn;
        private System.Windows.Forms.TableLayoutPanel PST_BottomPanel;
        private TSCustomButton PST_RemotePortBtn;
        private TSCustomButton PST_ExportBtn;
        private TSCustomButton PST_AutoRefreshBtn;
        private TSCustomButton PST_IpInfoBtn;
        private TSCustomButton PST_TextBoxClearBtn;
        private TSCustomCheckBox PST_SortMode;
        private System.Windows.Forms.FlowLayoutPanel PST_FilterPanel;
        internal System.Windows.Forms.Label PST_TotalCount;
        internal System.Windows.Forms.Label PST_SearchLabel;
        internal System.Windows.Forms.TextBox PST_TextBox;
        internal System.Windows.Forms.Label PST_TotalCount_V;
        private TSCustomCheckBox PST_OpenOnly;
        internal System.Windows.Forms.Label PST_RemoteHostLabel;
        internal System.Windows.Forms.TextBox PST_RemoteHostTextBox;
        internal System.Windows.Forms.Label PST_RemotePortsLabel;
        internal System.Windows.Forms.TextBox PST_RemotePortsTextBox;
        internal System.Windows.Forms.Label PST_RemoteTimeoutLabel;
        internal System.Windows.Forms.TextBox PST_RemoteTimeoutTextBox;
        private System.Windows.Forms.ToolTip PST_ToolTip;
        private System.Windows.Forms.FlowLayoutPanel PST_RemotePanel;
    }
}