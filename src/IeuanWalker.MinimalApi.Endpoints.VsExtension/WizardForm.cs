namespace IeuanWalker.MinimalApi.Endpoints.VsExtension;

#pragma warning disable CA2213 // Disposable fields should be disposed - Windows Forms designer handles disposal
public partial class WizardForm : Form
{
	private ComboBox httpVerbComboBox = null!;
	private CheckBox withRequestCheckBox = null!;
	private CheckBox withResponseCheckBox = null!;
	private CheckBox includeValidationCheckBox = null!;
	private TextBox routeTextBox = null!;
	private Button okButton = null!;
	private Button cancelButton = null!;
	private Label httpVerbLabel = null!;
	private Label routeLabel = null!;

	public string SelectedHttpVerb => httpVerbComboBox.SelectedItem?.ToString() ?? "Get";
	public bool WithRequest => withRequestCheckBox.Checked;
	public bool WithResponse => withResponseCheckBox.Checked;
	public bool IncludeValidation => includeValidationCheckBox.Checked;
	public string Route => routeTextBox.Text;

	public WizardForm()
	{
		InitializeComponent();
		SetupEventHandlers();
	}

	private void InitializeComponent()
	{
		this.httpVerbLabel = new Label();
		this.httpVerbComboBox = new ComboBox();
		this.withRequestCheckBox = new CheckBox();
		this.withResponseCheckBox = new CheckBox();
		this.includeValidationCheckBox = new CheckBox();
		this.routeLabel = new Label();
		this.routeTextBox = new TextBox();
		this.okButton = new Button();
		this.cancelButton = new Button();
		this.SuspendLayout();

		// httpVerbLabel
		this.httpVerbLabel.AutoSize = true;
		this.httpVerbLabel.Location = new System.Drawing.Point(20, 20);
		this.httpVerbLabel.Name = "httpVerbLabel";
		this.httpVerbLabel.Size = new System.Drawing.Size(70, 15);
		this.httpVerbLabel.TabIndex = 0;
		this.httpVerbLabel.Text = "HTTP Verb:";

		// httpVerbComboBox
		this.httpVerbComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
		this.httpVerbComboBox.FormattingEnabled = true;
		this.httpVerbComboBox.Items.AddRange(new object[] { "Get", "Post", "Put", "Delete", "Patch" });
		this.httpVerbComboBox.Location = new System.Drawing.Point(20, 40);
		this.httpVerbComboBox.Name = "httpVerbComboBox";
		this.httpVerbComboBox.Size = new System.Drawing.Size(350, 23);
		this.httpVerbComboBox.TabIndex = 1;
		this.httpVerbComboBox.SelectedIndex = 0;

		// withRequestCheckBox
		this.withRequestCheckBox.AutoSize = true;
		this.withRequestCheckBox.Checked = true;
		this.withRequestCheckBox.CheckState = CheckState.Checked;
		this.withRequestCheckBox.Location = new System.Drawing.Point(20, 80);
		this.withRequestCheckBox.Name = "withRequestCheckBox";
		this.withRequestCheckBox.Size = new System.Drawing.Size(120, 19);
		this.withRequestCheckBox.TabIndex = 2;
		this.withRequestCheckBox.Text = "Include Request";
		this.withRequestCheckBox.UseVisualStyleBackColor = true;

		// withResponseCheckBox
		this.withResponseCheckBox.AutoSize = true;
		this.withResponseCheckBox.Checked = true;
		this.withResponseCheckBox.CheckState = CheckState.Checked;
		this.withResponseCheckBox.Location = new System.Drawing.Point(20, 110);
		this.withResponseCheckBox.Name = "withResponseCheckBox";
		this.withResponseCheckBox.Size = new System.Drawing.Size(125, 19);
		this.withResponseCheckBox.TabIndex = 3;
		this.withResponseCheckBox.Text = "Include Response";
		this.withResponseCheckBox.UseVisualStyleBackColor = true;

		// includeValidationCheckBox
		this.includeValidationCheckBox.AutoSize = true;
		this.includeValidationCheckBox.Checked = true;
		this.includeValidationCheckBox.CheckState = CheckState.Checked;
		this.includeValidationCheckBox.Location = new System.Drawing.Point(40, 140);
		this.includeValidationCheckBox.Name = "includeValidationCheckBox";
		this.includeValidationCheckBox.Size = new System.Drawing.Size(200, 19);
		this.includeValidationCheckBox.TabIndex = 4;
		this.includeValidationCheckBox.Text = "Include Validation (FluentValidation)";
		this.includeValidationCheckBox.UseVisualStyleBackColor = true;

		// routeLabel
		this.routeLabel.AutoSize = true;
		this.routeLabel.Location = new System.Drawing.Point(20, 170);
		this.routeLabel.Name = "routeLabel";
		this.routeLabel.Size = new System.Drawing.Size(40, 15);
		this.routeLabel.TabIndex = 5;
		this.routeLabel.Text = "Route:";

		// routeTextBox
		this.routeTextBox.Location = new System.Drawing.Point(20, 190);
		this.routeTextBox.Name = "routeTextBox";
		this.routeTextBox.Size = new System.Drawing.Size(350, 23);
		this.routeTextBox.TabIndex = 6;
		this.routeTextBox.Text = "/api/endpoint";

		// okButton
		this.okButton.Location = new System.Drawing.Point(214, 230);
		this.okButton.Name = "okButton";
		this.okButton.Size = new System.Drawing.Size(75, 30);
		this.okButton.TabIndex = 7;
		this.okButton.Text = "OK";
		this.okButton.UseVisualStyleBackColor = true;
		this.okButton.Click += new EventHandler(this.OkButton_Click);

		// cancelButton
		this.cancelButton.DialogResult = DialogResult.Cancel;
		this.cancelButton.Location = new System.Drawing.Point(295, 230);
		this.cancelButton.Name = "cancelButton";
		this.cancelButton.Size = new System.Drawing.Size(75, 30);
		this.cancelButton.TabIndex = 8;
		this.cancelButton.Text = "Cancel";
		this.cancelButton.UseVisualStyleBackColor = true;
		this.cancelButton.Click += new EventHandler(this.CancelButton_Click);

		// WizardForm
		this.AcceptButton = this.okButton;
		this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
		this.AutoScaleMode = AutoScaleMode.Font;
		this.CancelButton = this.cancelButton;
		this.ClientSize = new System.Drawing.Size(390, 280);
		this.Controls.Add(this.cancelButton);
		this.Controls.Add(this.okButton);
		this.Controls.Add(this.routeTextBox);
		this.Controls.Add(this.routeLabel);
		this.Controls.Add(this.includeValidationCheckBox);
		this.Controls.Add(this.withResponseCheckBox);
		this.Controls.Add(this.withRequestCheckBox);
		this.Controls.Add(this.httpVerbComboBox);
		this.Controls.Add(this.httpVerbLabel);
		this.FormBorderStyle = FormBorderStyle.FixedDialog;
		this.MaximizeBox = false;
		this.MinimizeBox = false;
		this.Name = "WizardForm";
		this.StartPosition = FormStartPosition.CenterScreen;
		this.Text = "Create Endpoint";
		this.ResumeLayout(false);
		this.PerformLayout();
	}

	private void SetupEventHandlers()
	{
		withRequestCheckBox.CheckedChanged += WithRequestCheckBox_CheckedChanged;
		withResponseCheckBox.CheckedChanged += WithResponseCheckBox_CheckedChanged;
	}

	private void WithRequestCheckBox_CheckedChanged(object? sender, EventArgs e)
	{
		// Only enable validation checkbox if request is included
		includeValidationCheckBox.Enabled = withRequestCheckBox.Checked;
		if (!withRequestCheckBox.Checked)
		{
			includeValidationCheckBox.Checked = false;
		}
	}

	private void WithResponseCheckBox_CheckedChanged(object? sender, EventArgs e)
	{
		// If response is checked and includeValidation wasn't explicitly unchecked, check it
		if (withResponseCheckBox.Checked && withRequestCheckBox.Checked)
		{
			includeValidationCheckBox.Enabled = true;
		}
	}

	private void OkButton_Click(object? sender, EventArgs e)
	{
		if (string.IsNullOrWhiteSpace(routeTextBox.Text))
		{
			MessageBox.Show("Please enter a route.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}

		this.DialogResult = DialogResult.OK;
		this.Close();
	}

	private void CancelButton_Click(object? sender, EventArgs e)
	{
		this.DialogResult = DialogResult.Cancel;
		this.Close();
	}
}
#pragma warning restore CA2213 // Disposable fields should be disposed
