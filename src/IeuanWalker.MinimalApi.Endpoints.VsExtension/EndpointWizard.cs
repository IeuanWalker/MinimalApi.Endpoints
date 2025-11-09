using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;

namespace IeuanWalker.MinimalApi.Endpoints.VsExtension;

public class EndpointWizard : IWizard
{
	private WizardForm? _wizardForm;
	private bool _shouldAddProjectItem = true;

	public void BeforeOpeningFile(ProjectItem projectItem)
	{
	}

	public void ProjectFinishedGenerating(Project project)
	{
	}

	public void ProjectItemFinishedGenerating(ProjectItem projectItem)
	{
	}

	public void RunFinished()
	{
	}

	public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
	{
		try
		{
			_wizardForm = new WizardForm();
			DialogResult result = _wizardForm.ShowDialog();

			if (result == DialogResult.OK)
			{
				// Add wizard selections to replacement dictionary
				replacementsDictionary.Add("$httpVerb$", _wizardForm.SelectedHttpVerb);
				replacementsDictionary.Add("$withRequest$", _wizardForm.WithRequest.ToString().ToLowerInvariant());
				replacementsDictionary.Add("$withResponse$", _wizardForm.WithResponse.ToString().ToLowerInvariant());
				replacementsDictionary.Add("$includeValidation$", _wizardForm.IncludeValidation.ToString().ToLowerInvariant());
				replacementsDictionary.Add("$route$", _wizardForm.Route);

				_shouldAddProjectItem = true;
			}
			else
			{
				_shouldAddProjectItem = false;
				throw new WizardCancelledException("The wizard was cancelled by the user.");
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Error in wizard: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			throw new WizardCancelledException("The wizard was cancelled due to an error.");
		}
	}

	public bool ShouldAddProjectItem(string filePath)
	{
		if (!_shouldAddProjectItem || _wizardForm is null)
		{
			return false;
		}

		// Only add RequestModel.cs if withRequest is true
		if (filePath.EndsWith("RequestModel.cs", StringComparison.OrdinalIgnoreCase))
		{
			return _wizardForm.WithRequest;
		}

		// Only add ResponseModel.cs if withResponse is true
		if (filePath.EndsWith("ResponseModel.cs", StringComparison.OrdinalIgnoreCase))
		{
			return _wizardForm.WithResponse;
		}

		// Always add the endpoint file
		return true;
	}
}
