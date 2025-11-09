# Endpoint Template Wizard UI

This document describes the user interface of the endpoint template wizard.

## Wizard Form Layout

```
┌────────────────────────────────────────────────────────┐
│  Create Endpoint                                    [X] │
├────────────────────────────────────────────────────────┤
│                                                         │
│  HTTP Verb:                                            │
│  ┌──────────────────────────────────────────────────┐  │
│  │ Get                                           ▼  │  │
│  └──────────────────────────────────────────────────┘  │
│                                                         │
│  ☑ Include Request                                     │
│                                                         │
│  ☑ Include Response                                    │
│                                                         │
│    ☑ Include Validation (FluentValidation)            │
│                                                         │
│  Route:                                                │
│  ┌──────────────────────────────────────────────────┐  │
│  │ /api/endpoint                                    │  │
│  └──────────────────────────────────────────────────┘  │
│                                                         │
│                                     ┌────┐  ┌────────┐ │
│                                     │ OK │  │ Cancel │ │
│                                     └────┘  └────────┘ │
└────────────────────────────────────────────────────────┘
```

## Form Controls

### HTTP Verb Dropdown
- **Type**: ComboBox (DropDownList style)
- **Options**: Get, Post, Put, Delete, Patch
- **Default**: Get
- **Description**: Selects which HTTP method the endpoint will handle

### Include Request Checkbox
- **Type**: CheckBox
- **Default**: Checked
- **Description**: When checked, generates a RequestModel.cs file
- **Behavior**: 
  - Disables "Include Validation" checkbox when unchecked
  - Automatically unchecks "Include Validation" when unchecked

### Include Response Checkbox
- **Type**: CheckBox
- **Default**: Checked
- **Description**: When checked, generates a ResponseModel.cs file

### Include Validation Checkbox
- **Type**: CheckBox
- **Default**: Checked
- **Indented**: Yes (20px left indent to show it's a sub-option)
- **Description**: When checked, adds FluentValidation validator class to RequestModel.cs
- **Dependencies**: 
  - Only enabled when "Include Request" is checked
  - Automatically disabled/unchecked when "Include Request" is unchecked

### Route TextBox
- **Type**: TextBox
- **Default**: "/api/endpoint"
- **Description**: The route path for the endpoint
- **Validation**: Must not be empty (shows warning message on OK if empty)

### OK Button
- **Type**: Button
- **Description**: Confirms selections and generates files
- **Behavior**:
  - Validates that route is not empty
  - Closes dialog with DialogResult.OK
  - Triggers file generation

### Cancel Button
- **Type**: Button  
- **Description**: Cancels template creation
- **Behavior**: Closes dialog with DialogResult.Cancel, no files generated

## Generated Files Based on Selections

| Include Request | Include Response | Include Validation | Files Generated |
|----------------|------------------|-------------------|-----------------|
| ✓ | ✓ | ✓ | Endpoint.cs, RequestModel.cs (with validator), ResponseModel.cs |
| ✓ | ✓ | ✗ | Endpoint.cs, RequestModel.cs (no validator), ResponseModel.cs |
| ✓ | ✗ | ✓ | Endpoint.cs, RequestModel.cs (with validator) |
| ✓ | ✗ | ✗ | Endpoint.cs, RequestModel.cs (no validator) |
| ✗ | ✓ | N/A | Endpoint.cs, ResponseModel.cs |
| ✗ | ✗ | N/A | Endpoint.cs only |

## Interface Selection Logic

The endpoint class implements different interfaces based on selections:

| Request | Response | Interface |
|---------|----------|-----------|
| ✓ | ✓ | `IEndpoint<RequestModel, Results<Ok<ResponseModel>, NotFound>>` |
| ✓ | ✗ | `IEndpointWithoutResponse<RequestModel>` |
| ✗ | ✓ | `IEndpointWithoutRequest<Results<Ok<ResponseModel>, NotFound>>` |
| ✗ | ✗ | `IEndpoint` |

## Example Usage Scenarios

### Scenario 1: Full CRUD Endpoint with Validation
**User Selections:**
- HTTP Verb: Post
- Include Request: ✓
- Include Response: ✓
- Include Validation: ✓
- Route: /api/users

**Result:** Creates a POST endpoint with request/response models and FluentValidation

### Scenario 2: Simple GET Endpoint
**User Selections:**
- HTTP Verb: Get
- Include Request: ✗
- Include Response: ✓
- Route: /api/users/{id}

**Result:** Creates a GET endpoint with only response model (no request model needed for route parameters)

### Scenario 3: DELETE Endpoint
**User Selections:**
- HTTP Verb: Delete
- Include Request: ✗
- Include Response: ✗
- Route: /api/users/{id}

**Result:** Creates a simple DELETE endpoint (route parameters handled via route, no body needed)

## User Experience Considerations

### Validation
- ✅ Route field must not be empty
- ✅ Clear visual feedback for enabled/disabled options
- ✅ Intuitive indentation shows option relationships
- ✅ Helpful default values speed up common scenarios

### Usability
- Tab order follows logical flow (top to bottom)
- Enter key triggers OK button
- Escape key triggers Cancel button
- Clear visual grouping of related options
- Checkbox labels are clickable

### Accessibility
- All controls have proper tab stops
- Checkbox labels provide clear descriptions
- Visual hierarchy with indentation
- Keyboard shortcuts for common actions
