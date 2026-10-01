// ==========================================================================
// EventEase - Dynamic Custom Questions Builder Script
// ==========================================================================

document.addEventListener('DOMContentLoaded', function () {
    var container = document.getElementById('customFieldsContainer');
    var addBtn = document.getElementById('btnAddCustomField');
    var emptyNotice = document.getElementById('noCustomFieldsNotice');

    if (!container || !addBtn) return;

    // Existing custom field values can come from persisted organizer input.
    // Escape them before placing them in the row template to prevent stored XSS.
    function escapeHtml(value) {
        return String(value == null ? '' : value).replace(/[&<>"']/g, function (character) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[character];
        });
    }

    // Helper: Re-index field names for ASP.NET MVC model binding
    function reindexCustomFields() {
        var rows = container.querySelectorAll('.custom-field-row');
        if (rows.length === 0) {
            if (emptyNotice) emptyNotice.style.display = 'block';
        } else {
            if (emptyNotice) emptyNotice.style.display = 'none';
        }

        rows.forEach(function (row, index) {
            row.setAttribute('data-index', index);
            row.querySelector('.field-index-badge').innerText = "#" + (index + 1);

            var idInput = row.querySelector('.field-id');
            if (idInput) idInput.name = `CustomFields[${index}].Id`;

            var labelInput = row.querySelector('.field-label');
            if (labelInput) labelInput.name = `CustomFields[${index}].Label`;

            var typeSelect = row.querySelector('.field-type');
            if (typeSelect) typeSelect.name = `CustomFields[${index}].FieldType`;

            var requiredCheck = row.querySelector('.field-required');
            if (requiredCheck) {
                requiredCheck.name = `CustomFields[${index}].Required`;
                requiredCheck.id = `CustomFields_${index}__Required`;
                var label = row.querySelector('.field-required-label');
                if (label) label.setAttribute('for', `CustomFields_${index}__Required`);
            }

            var optionsInput = row.querySelector('.field-options');
            if (optionsInput) optionsInput.name = `CustomFields[${index}].Options`;
        });
    }

    // Function to add a new question row
    window.addCustomFieldRow = function (labelVal, typeVal, requiredVal, optionsVal) {
        var index = container.querySelectorAll('.custom-field-row').length;
        var row = document.createElement('div');
        row.className = 'custom-field-row card card-modern p-3 mb-3';
        row.setAttribute('data-index', index);

        var isDropdown = (typeVal === 'Dropdown');

        row.innerHTML = `
            <div class="row g-2 align-items-center">
                <div class="col-auto">
                    <span class="badge badge-neutral font-mono field-index-badge">#${index + 1}</span>
                </div>
                <div class="col-md-4">
                    <label class="form-label small text-muted mb-1">Question / Label <span class="text-danger">*</span></label>
                    <input type="text" class="form-control form-control-sm field-label" 
                           placeholder="e.g. Dietary Restriction, Student Number" 
                           value="${escapeHtml(labelVal)}" required />
                </div>
                <div class="col-md-3">
                    <label class="form-label small text-muted mb-1">Answer Type</label>
                    <select class="form-select form-select-sm field-type">
                        <option value="Text" ${typeVal === 'Text' ? 'selected' : ''}>Short Text</option>
                        <option value="Dropdown" ${typeVal === 'Dropdown' ? 'selected' : ''}>Dropdown (Options)</option>
                        <option value="Number" ${typeVal === 'Number' ? 'selected' : ''}>Number</option>
                        <option value="Checkbox" ${typeVal === 'Checkbox' ? 'selected' : ''}>Yes/No Checkbox</option>
                    </select>
                </div>
                <div class="col-md-2 d-flex align-items-center pt-md-3">
                    <div class="form-check form-switch">
                        <input class="form-check-input field-required" type="checkbox" value="true" ${requiredVal ? 'checked' : ''} />
                        <label class="form-check-label small field-required-label">Required</label>
                    </div>
                </div>
                <div class="col-md-auto ms-auto pt-md-3">
                    <button type="button" class="btn btn-outline-danger btn-sm btn-remove-field" title="Remove question">
                        <i class="bi bi-trash"></i>
                    </button>
                </div>
            </div>
            <div class="row g-2 mt-2 options-row" style="${isDropdown ? 'display: flex;' : 'display: none;'}">
                <div class="col-md-11 offset-md-1">
                    <label class="form-label small text-muted mb-1">Dropdown Choices (comma-separated)</label>
                    <input type="text" class="form-control form-control-sm field-options" 
                           placeholder="e.g. Vegetarian, Vegan, Halal, Gluten-Free" 
                           value="${escapeHtml(optionsVal)}" />
                </div>
            </div>
        `;

        container.appendChild(row);

        // Attach change event on field type to show/hide options
        var typeSelect = row.querySelector('.field-type');
        var optionsRow = row.querySelector('.options-row');
        typeSelect.addEventListener('change', function () {
            if (this.value === 'Dropdown') {
                optionsRow.style.display = 'flex';
            } else {
                optionsRow.style.display = 'none';
            }
        });

        // Attach remove event
        row.querySelector('.btn-remove-field').addEventListener('click', function () {
            row.remove();
            reindexCustomFields();
        });

        reindexCustomFields();
    };

    addBtn.addEventListener('click', function () {
        window.addCustomFieldRow('', 'Text', false, '');
    });

    // Quick presets
    var presetButtons = document.querySelectorAll('.btn-preset-question');
    presetButtons.forEach(function (btn) {
        btn.addEventListener('click', function () {
            var label = this.getAttribute('data-label');
            var type = this.getAttribute('data-type');
            var options = this.getAttribute('data-options') || '';
            var req = this.getAttribute('data-required') === 'true';
            window.addCustomFieldRow(label, type, req, options);
        });
    });

    // Attach listener for existing rows (Edit mode)
    var existingRows = container.querySelectorAll('.custom-field-row');
    existingRows.forEach(function (row) {
        var typeSelect = row.querySelector('.field-type');
        var optionsRow = row.querySelector('.options-row');
        if (typeSelect && optionsRow) {
            typeSelect.addEventListener('change', function () {
                if (this.value === 'Dropdown') {
                    optionsRow.style.display = 'flex';
                } else {
                    optionsRow.style.display = 'none';
                }
            });
        }

        var removeBtn = row.querySelector('.btn-remove-field');
        if (removeBtn) {
            removeBtn.addEventListener('click', function () {
                row.remove();
                reindexCustomFields();
            });
        }
    });

    reindexCustomFields();
});
