namespace IeuanWalker.MinimalApi.Endpoints.VsExtension;

#pragma warning disable CA2213 // Disposable fields should be disposed - Windows Forms designer handles disposal
public partial class WizardForm : Form
{
	ComboBox _httpVerbComboBox = null!;
	CheckBox _withRequestCheckBox = null!;
	CheckBox _withResponseCheckBox = null!;
	CheckBox _includeValidationCheckBox = null!;
	TextBox _routeTextBox = null!;
	Button _okButton = null!;
	Button _cancelButton = null!;
	Label _httpVerbLabel = null!;
	Label _routeLabel = null!;

	public string SelectedHttpVerb => _httpVerbComboBox.SelectedItem?.ToString() ?? "Get";
	public bool WithRequest => _withRequestCheckBox.Checked;
	public bool WithResponse => _withResponseCheckBox.Checked;
	public bool IncludeValidation => _includeValidationCheckBox.Checked;
	public string Route => _routeTextBox.Text;

	public WizardForm()
	{
		InitializeComponent();
		SetupEventHandlers();
	}

	void InitializeComponent()
	{
		_httpVerbLabel = new Label();
		_httpVerbComboBox = new ComboBox();
		_withRequestCheckBox = new CheckBox();
		_withResponseCheckBox = new CheckBox();
		_includeValidationCheckBox = new CheckBox();
		_routeLabel = new Label();
		_routeTextBox = new TextBox();
		_okButton = new Button();
		_cancelButton = new Button();
		SuspendLayout();

		// httpVerbLabel
		_httpVerbLabel.AutoSize = true;
		_httpVerbLabel.Location = new Point(20, 20);
		_httpVerbLabel.Name = "httpVerbLabel";
		_httpVerbLabel.Size = new Size(70, 15);
		_httpVerbLabel.TabIndex = 0;
		_httpVerbLabel.Text = "HTTP Verb:";

		// httpVerbComboBox
		_httpVerbComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
		_httpVerbComboBox.FormattingEnabled = true;
		_httpVerbComboBox.Items.AddRange(["Get", "Post", "Put", "Delete", "Patch"]);
		_httpVerbComboBox.Location = new Point(20, 40);
		_httpVerbComboBox.Name = "httpVerbComboBox";
		_httpVerbComboBox.Size = new Size(350, 23);
		_httpVerbComboBox.TabIndex = 1;
		_httpVerbComboBox.SelectedIndex = 0;

		// withRequestCheckBox
		_withRequestCheckBox.AutoSize = true;
		_withRequestCheckBox.Checked = true;
		_withRequestCheckBox.CheckState = CheckState.Checked;
		_withRequestCheckBox.Location = new Point(20, 80);
		_withRequestCheckBox.Name = "withRequestCheckBox";
		_withRequestCheckBox.Size = new Size(120, 19);
		_withRequestCheckBox.TabIndex = 2;
		_withRequestCheckBox.Text = "Include Request";
		_withRequestCheckBox.UseVisualStyleBackColor = true;

		// withResponseCheckBox
		_withResponseCheckBox.AutoSize = true;
		_withResponseCheckBox.Checked = true;
		_withResponseCheckBox.CheckState = CheckState.Checked;
		_withResponseCheckBox.Location = new Point(20, 110);
		_withResponseCheckBox.Name = "withResponseCheckBox";
		_withResponseCheckBox.Size = new Size(125, 19);
		_withResponseCheckBox.TabIndex = 3;
		_withResponseCheckBox.Text = "Include Response";
		_withResponseCheckBox.UseVisualStyleBackColor = true;

		// includeValidationCheckBox
		_includeValidationCheckBox.AutoSize = true;
		_includeValidationCheckBox.Checked = true;
		_includeValidationCheckBox.CheckState = CheckState.Checked;
		_includeValidationCheckBox.Location = new Point(40, 140);
		_includeValidationCheckBox.Name = "includeValidationCheckBox";
		_includeValidationCheckBox.Size = new Size(200, 19);
		_includeValidationCheckBox.TabIndex = 4;
		_includeValidationCheckBox.Text = "Include Validation (FluentValidation)";
		_includeValidationCheckBox.UseVisualStyleBackColor = true;

		// routeLabel
		_routeLabel.AutoSize = true;
		_routeLabel.Location = new Point(20, 170);
		_routeLabel.Name = "routeLabel";
		_routeLabel.Size = new Size(40, 15);
		_routeLabel.TabIndex = 5;
		_routeLabel.Text = "Route:";

		// routeTextBox
		_routeTextBox.Location = new Point(20, 190);
		_routeTextBox.Name = "routeTextBox";
		_routeTextBox.Size = new Size(350, 23);
		_routeTextBox.TabIndex = 6;
		_routeTextBox.Text = "/api/endpoint";

		// okButton
		_okButton.Location = new Point(214, 230);
		_okButton.Name = "okButton";
		_okButton.Size = new Size(75, 30);
		_okButton.TabIndex = 7;
		_okButton.Text = "OK";
		_okButton.UseVisualStyleBackColor = true;
		_okButton.Click += OkButton_Click;

		// cancelButton
		_cancelButton.DialogResult = DialogResult.Cancel;
		_cancelButton.Location = new Point(295, 230);
		_cancelButton.Name = "cancelButton";
		_cancelButton.Size = new Size(75, 30);
		_cancelButton.TabIndex = 8;
		_cancelButton.Text = "Cancel";
		_cancelButton.UseVisualStyleBackColor = true;
		_cancelButton.Click += CancelButton_Click;

		// WizardForm
		AcceptButton = _okButton;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		CancelButton = _cancelButton;
		ClientSize = new Size(390, 280);
		Controls.Add(_cancelButton);
		Controls.Add(_okButton);
		Controls.Add(_routeTextBox);
		Controls.Add(_routeLabel);
		Controls.Add(_includeValidationCheckBox);
		Controls.Add(_withResponseCheckBox);
		Controls.Add(_withRequestCheckBox);
		Controls.Add(_httpVerbComboBox);
		Controls.Add(_httpVerbLabel);
		FormBorderStyle = FormBorderStyle.FixedDialog;
		MaximizeBox = false;
		MinimizeBox = false;
		Name = "WizardForm";
		StartPosition = FormStartPosition.CenterScreen;
		Text = "Create Endpoint";
		ResumeLayout(false);
		PerformLayout();
	}

	void SetupEventHandlers()
	{
		_withRequestCheckBox.CheckedChanged += WithRequestCheckBox_CheckedChanged;
		_withResponseCheckBox.CheckedChanged += WithResponseCheckBox_CheckedChanged;
	}

	void WithRequestCheckBox_CheckedChanged(object? sender, EventArgs e)
	{
		// Only enable validation checkbox if request is included
		_includeValidationCheckBox.Enabled = _withRequestCheckBox.Checked;
		if (!_withRequestCheckBox.Checked)
		{
			_includeValidationCheckBox.Checked = false;
		}
	}

	void WithResponseCheckBox_CheckedChanged(object? sender, EventArgs e)
	{
		// If response is checked and includeValidation wasn't explicitly unchecked, check it
		if (_withResponseCheckBox.Checked && _withRequestCheckBox.Checked)
		{
			_includeValidationCheckBox.Enabled = true;
		}
	}

	void OkButton_Click(object? sender, EventArgs e)
	{
		if (string.IsNullOrWhiteSpace(_routeTextBox.Text))
		{
			MessageBox.Show("Please enter a route.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}

		DialogResult = DialogResult.OK;
		Close();
	}

	void CancelButton_Click(object? sender, EventArgs e)
	{
		DialogResult = DialogResult.Cancel;
		Close();
	}
}
#pragma warning restore CA2213 // Disposable fields should be disposed
