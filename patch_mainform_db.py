import sys

def process_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    # Add private readonly DatabaseService _dbService;
    content = content.replace(
        "private CancellationTokenSource? _cts;",
        "private CancellationTokenSource? _cts;\n    private readonly Services.DatabaseService _dbService;"
    )

    # Initialize it in constructor
    content = content.replace(
        "_adb = new AdbManager();",
        "_adb = new AdbManager();\n        _dbService = new Services.DatabaseService();\n        try { _dbService.InitializeDatabase(); } catch (Exception ex) { Logger.Error(\"Lỗi khởi tạo DB\", ex); }"
    )

    # Modify LoadAutoSavedWorkflow
    old_load = """    private void LoadAutoSavedWorkflow()
    {
        var path = AutoSaveWorkflowPath;
        if (!File.Exists(path)) return;

        try
        {
            var document = WorkflowEngine.LoadWorkflowDocument(path);
            _workflowSteps.Clear();
            _workflowSteps.AddRange(document.Steps);
            _variables.RemoveAll(variable => !variable.IsBuiltIn);
            _variables.AddRange(document.Variables.Where(variable => !variable.IsBuiltIn));
            _jobs.Clear();
            _jobs.AddRange(document.Products ?? []);
            RefreshVariableList();
            RefreshWorkflow();
            RefreshJobGrid();

            var selectedIndex = _workflowSteps.Count > 0 ? 0 : -1;
            workflowCanvas.SelectStep(selectedIndex);
            LoadStepConfig(selectedIndex);
            Logger.Info($"Đã khôi phục workflow và {_jobs.Count} sản phẩm tự động.");
        }
        catch (Exception ex)
        {
            Logger.Warn($"Không thể khôi phục workflow tự động: {ex.Message}");
        }
    }"""
    
    new_load = """    private void LoadAutoSavedWorkflow()
    {
        var path = AutoSaveWorkflowPath;
        if (File.Exists(path))
        {
            try
            {
                var document = WorkflowEngine.LoadWorkflowDocument(path);
                _workflowSteps.Clear();
                _workflowSteps.AddRange(document.Steps);
                _variables.RemoveAll(variable => !variable.IsBuiltIn);
                _variables.AddRange(document.Variables.Where(variable => !variable.IsBuiltIn));
                RefreshVariableList();
                RefreshWorkflow();

                var selectedIndex = _workflowSteps.Count > 0 ? 0 : -1;
                workflowCanvas.SelectStep(selectedIndex);
                LoadStepConfig(selectedIndex);
                Logger.Info("Đã khôi phục workflow tự động.");
            }
            catch (Exception ex)
            {
                Logger.Warn($"Không thể khôi phục workflow tự động: {ex.Message}");
            }
        }
        
        try 
        {
            _jobs.Clear();
            _jobs.AddRange(_dbService.GetAllJobs());
            RefreshJobGrid();
            Logger.Info($"Đã tải {_jobs.Count} sản phẩm từ CSDL.");
        }
        catch (Exception ex)
        {
            Logger.Error($"Không thể tải dữ liệu từ CSDL: {ex.Message}", ex);
        }
    }"""
    content = content.replace(old_load, new_load)

    # Change LoadWorkflow to not clear/overwrite _jobs
    content = content.replace(
        """            _variables.AddRange(document.Variables.Where(v => !v.IsBuiltIn));
            _jobs.Clear();
            _jobs.AddRange(document.Products ?? []);
            RefreshVariableList();
            RefreshWorkflow();
            RefreshJobGrid();""",
        """            _variables.AddRange(document.Variables.Where(v => !v.IsBuiltIn));
            RefreshVariableList();
            RefreshWorkflow();"""
    )
    content = content.replace(
        "SetStatus($\"Đã mở workflow chứa {_workflowSteps.Count} bước và {_jobs.Count} sản phẩm\");",
        "SetStatus($\"Đã mở workflow chứa {_workflowSteps.Count} bước\");"
    )

    # Update ImportExcel
    content = content.replace(
        "            RefreshJobGrid();\n            SaveAutoSavedWorkflow();",
        "            RefreshJobGrid();\n            _dbService.ReplaceAllJobs(_jobs);\n            SaveAutoSavedWorkflow();"
    )

    # Update AddProduct
    content = content.replace(
        "        _jobs.Add(newItem);\n        RefreshJobGrid();\n        SaveAutoSavedWorkflow();",
        "        _dbService.SaveJob(newItem);\n        _jobs.Add(newItem);\n        RefreshJobGrid();\n        SaveAutoSavedWorkflow();"
    )

    # Update DeleteProduct
    content = content.replace(
        "        _jobs.RemoveAt(index);\n        RefreshJobGrid();\n        SaveAutoSavedWorkflow();",
        "        var job = _jobs[index];\n        _dbService.DeleteJob(job.Id);\n        _jobs.RemoveAt(index);\n        RefreshJobGrid();\n        SaveAutoSavedWorkflow();"
    )

    # Update ResetProductStatuses
    content = content.replace(
        "        RefreshJobGrid();\n        SaveAutoSavedWorkflow();\n        SetStatus(\"Đã đặt lại trạng thái các sản phẩm\");",
        "        RefreshJobGrid();\n        foreach(var job in _jobs) _dbService.UpdateJob(job);\n        SaveAutoSavedWorkflow();\n        SetStatus(\"Đã đặt lại trạng thái các sản phẩm\");"
    )

    # Update ApplyProductChange
    content = content.replace(
        "        productListControl.UpdateProduct(e.Index, updated);\n        SaveAutoSavedWorkflow();",
        "        productListControl.UpdateProduct(e.Index, updated);\n        _dbService.UpdateJob(updated);\n        SaveAutoSavedWorkflow();"
    )
    
    # Update UpdateJobRow
    content = content.replace(
        "        productListControl.UpdateProduct(index, _jobs[index]);\n    }",
        "        productListControl.UpdateProduct(index, _jobs[index]);\n        _dbService.UpdateJob(_jobs[index]);\n    }"
    )
    
    # Add Export DB / Import DB buttons
    # In MainForm.cs there's a toolbar in ProductListControl. I should handle the UI updates there.

    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)
    print("MainForm.cs patched for DatabaseService.")

if __name__ == "__main__":
    process_file("MainForm.cs")
