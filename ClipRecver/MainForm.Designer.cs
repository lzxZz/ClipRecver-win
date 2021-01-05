namespace ClipRecver {
    partial class MainForm {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            this.lbl监听IP文本 = new System.Windows.Forms.Label();
            this.lbl端口文本 = new System.Windows.Forms.Label();
            this.nud端口 = new System.Windows.Forms.NumericUpDown();
            this.cbx监听IP = new System.Windows.Forms.ComboBox();
            this.pbx配置QR码 = new System.Windows.Forms.PictureBox();
            this.lbl密码文本 = new System.Windows.Forms.Label();
            this.tbx密码 = new System.Windows.Forms.TextBox();
            this.btn保存设置 = new System.Windows.Forms.Button();
            this.btn撤销改动 = new System.Windows.Forms.Button();
            this.nic托盘图标 = new System.Windows.Forms.NotifyIcon(this.components);
            this.cms托盘菜单 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsmi显示隐藏 = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmi退出 = new System.Windows.Forms.ToolStripMenuItem();
            this.btn开始停止 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.nud端口)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbx配置QR码)).BeginInit();
            this.cms托盘菜单.SuspendLayout();
            this.SuspendLayout();
            // 
            // lbl监听IP文本
            // 
            this.lbl监听IP文本.AutoSize = true;
            this.lbl监听IP文本.Location = new System.Drawing.Point(12, 14);
            this.lbl监听IP文本.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.lbl监听IP文本.Name = "lbl监听IP文本";
            this.lbl监听IP文本.Size = new System.Drawing.Size(57, 21);
            this.lbl监听IP文本.TabIndex = 0;
            this.lbl监听IP文本.Text = "监听IP";
            // 
            // lbl端口文本
            // 
            this.lbl端口文本.AutoSize = true;
            this.lbl端口文本.Location = new System.Drawing.Point(27, 56);
            this.lbl端口文本.Name = "lbl端口文本";
            this.lbl端口文本.Size = new System.Drawing.Size(42, 21);
            this.lbl端口文本.TabIndex = 2;
            this.lbl端口文本.Text = "端口";
            // 
            // nud端口
            // 
            this.nud端口.Location = new System.Drawing.Point(71, 53);
            this.nud端口.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.nud端口.Minimum = new decimal(new int[] {
            1024,
            0,
            0,
            0});
            this.nud端口.Name = "nud端口";
            this.nud端口.Size = new System.Drawing.Size(75, 29);
            this.nud端口.TabIndex = 3;
            this.nud端口.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.nud端口.Value = new decimal(new int[] {
            7814,
            0,
            0,
            0});
            // 
            // cbx监听IP
            // 
            this.cbx监听IP.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbx监听IP.FormattingEnabled = true;
            this.cbx监听IP.Location = new System.Drawing.Point(71, 11);
            this.cbx监听IP.Name = "cbx监听IP";
            this.cbx监听IP.Size = new System.Drawing.Size(151, 29);
            this.cbx监听IP.TabIndex = 4;
            // 
            // pbx配置QR码
            // 
            this.pbx配置QR码.BackColor = System.Drawing.Color.White;
            this.pbx配置QR码.Location = new System.Drawing.Point(233, 11);
            this.pbx配置QR码.Name = "pbx配置QR码";
            this.pbx配置QR码.Size = new System.Drawing.Size(160, 160);
            this.pbx配置QR码.TabIndex = 5;
            this.pbx配置QR码.TabStop = false;
            // 
            // lbl密码文本
            // 
            this.lbl密码文本.AutoSize = true;
            this.lbl密码文本.Location = new System.Drawing.Point(27, 98);
            this.lbl密码文本.Name = "lbl密码文本";
            this.lbl密码文本.Size = new System.Drawing.Size(42, 21);
            this.lbl密码文本.TabIndex = 6;
            this.lbl密码文本.Text = "密码";
            // 
            // tbx密码
            // 
            this.tbx密码.Location = new System.Drawing.Point(71, 95);
            this.tbx密码.MaxLength = 16;
            this.tbx密码.Name = "tbx密码";
            this.tbx密码.Size = new System.Drawing.Size(151, 29);
            this.tbx密码.TabIndex = 7;
            this.tbx密码.Leave += new System.EventHandler(this.tbx密码_Leave);
            // 
            // btn保存设置
            // 
            this.btn保存设置.Enabled = false;
            this.btn保存设置.Location = new System.Drawing.Point(10, 136);
            this.btn保存设置.Name = "btn保存设置";
            this.btn保存设置.Size = new System.Drawing.Size(102, 36);
            this.btn保存设置.TabIndex = 8;
            this.btn保存设置.Text = "保存设置";
            this.btn保存设置.UseVisualStyleBackColor = true;
            this.btn保存设置.Click += new System.EventHandler(this.btn保存设置_Click);
            // 
            // btn撤销改动
            // 
            this.btn撤销改动.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btn撤销改动.Enabled = false;
            this.btn撤销改动.Location = new System.Drawing.Point(121, 136);
            this.btn撤销改动.Name = "btn撤销改动";
            this.btn撤销改动.Size = new System.Drawing.Size(102, 36);
            this.btn撤销改动.TabIndex = 9;
            this.btn撤销改动.Text = "撤销改动";
            this.btn撤销改动.UseVisualStyleBackColor = true;
            this.btn撤销改动.Click += new System.EventHandler(this.btn撤销改动_Click);
            // 
            // nic托盘图标
            // 
            this.nic托盘图标.ContextMenuStrip = this.cms托盘菜单;
            this.nic托盘图标.Icon = global::ClipRecver.Properties.Resources.MainIcon;
            this.nic托盘图标.Text = "ClipRecver";
            this.nic托盘图标.Visible = true;
            this.nic托盘图标.DoubleClick += new System.EventHandler(this.nic托盘图标_DoubleClick);
            // 
            // cms托盘菜单
            // 
            this.cms托盘菜单.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmi显示隐藏,
            this.tsmi退出});
            this.cms托盘菜单.Name = "cms托盘菜单";
            this.cms托盘菜单.Size = new System.Drawing.Size(140, 48);
            // 
            // tsmi显示隐藏
            // 
            this.tsmi显示隐藏.Name = "tsmi显示隐藏";
            this.tsmi显示隐藏.Size = new System.Drawing.Size(139, 22);
            this.tsmi显示隐藏.Text = "显示设置(&S)";
            this.tsmi显示隐藏.Click += new System.EventHandler(this.tsmi显示隐藏_Click);
            // 
            // tsmi退出
            // 
            this.tsmi退出.Name = "tsmi退出";
            this.tsmi退出.Size = new System.Drawing.Size(139, 22);
            this.tsmi退出.Text = "退出(&Q)";
            this.tsmi退出.Click += new System.EventHandler(this.tsmi退出_Click);
            // 
            // btn开始停止
            // 
            this.btn开始停止.Location = new System.Drawing.Point(158, 52);
            this.btn开始停止.Name = "btn开始停止";
            this.btn开始停止.Size = new System.Drawing.Size(64, 31);
            this.btn开始停止.TabIndex = 10;
            this.btn开始停止.Text = "停止";
            this.btn开始停止.UseVisualStyleBackColor = true;
            this.btn开始停止.Click += new System.EventHandler(this.btn开始停止_Click);
            // 
            // MainForm
            // 
            this.AcceptButton = this.btn保存设置;
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btn撤销改动;
            this.ClientSize = new System.Drawing.Size(404, 181);
            this.Controls.Add(this.btn开始停止);
            this.Controls.Add(this.btn撤销改动);
            this.Controls.Add(this.btn保存设置);
            this.Controls.Add(this.tbx密码);
            this.Controls.Add(this.lbl密码文本);
            this.Controls.Add(this.pbx配置QR码);
            this.Controls.Add(this.cbx监听IP);
            this.Controls.Add(this.nud端口);
            this.Controls.Add(this.lbl端口文本);
            this.Controls.Add(this.lbl监听IP文本);
            this.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = global::ClipRecver.Properties.Resources.MainIcon;
            this.Margin = new System.Windows.Forms.Padding(5);
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "ClipRecver 设置";
            this.Shown += new System.EventHandler(this.MainForm_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.nud端口)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbx配置QR码)).EndInit();
            this.cms托盘菜单.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbl监听IP文本;
        private System.Windows.Forms.Label lbl端口文本;
        private System.Windows.Forms.NumericUpDown nud端口;
        private System.Windows.Forms.ComboBox cbx监听IP;
        private System.Windows.Forms.PictureBox pbx配置QR码;
        private System.Windows.Forms.Label lbl密码文本;
        private System.Windows.Forms.TextBox tbx密码;
        private System.Windows.Forms.Button btn保存设置;
        private System.Windows.Forms.Button btn撤销改动;
        private System.Windows.Forms.NotifyIcon nic托盘图标;
        private System.Windows.Forms.ContextMenuStrip cms托盘菜单;
        private System.Windows.Forms.ToolStripMenuItem tsmi显示隐藏;
        private System.Windows.Forms.ToolStripMenuItem tsmi退出;
        private System.Windows.Forms.Button btn开始停止;
    }
}

