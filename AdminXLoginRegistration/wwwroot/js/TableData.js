
$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    $.ajax({
        url: '/AdminArea/Admin/GetUser',
        type: 'GET',
        dataType: 'json',
        success: OnSuccess
    });
}

function OnSuccess(response) {
    $('#_dataTable').DataTable({
        bProcessing: true,
        bLengthChange: true,
        lengthMenu: [[5, 10, 25, -1], [5, 10, 25, "All"]],
        bFilter: true,
        bSort: true,
        bPaginate: true,
        destroy: true,
        data: response,
        columns: [
            { data: 'email' },
            {
                data: 'userRoles',
                render: function (data, type, row) {
                    return data && data.length > 0 ? data.join(', ') : 'No Role';
                }
            },
            {
                data: 'userId',
                "render": function (data) {
                    return `<div class="btn-group" role="group">
                    <button class="btn btn-sm btn-primary" onclick="showRoleEditor('${data}', this)">
                        <i class="bi bi-pencil-square"></i> Edit
                    </button>
                    <button class="btn btn-sm btn-danger" onclick="deleteUser('${data}')">
                        <i class="bi bi-trash-fill"></i> Delete
                    </button>
                </div>`
                }
            }
        ]
    });
}

function showRoleEditor(userId, buttonElement) {
    $.ajax({
        url: `/AdminArea/Admin/GetRole?id=${userId}`,
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            const $btn = $(buttonElement);
            const $cell = $btn.closest('td');
            const roleOptions = data.allRoles.map(r =>
                `<option value="${r}" ${data.userRoles.includes(r) ? 'selected' : ''}>${r}</option>`
            ).join('');
            const dropdown = `<select class="form-select form-select-sm role-selector">${roleOptions}</select>`;
            const saveBtn = `<button class="btn btn-sm btn-success ms-2" onclick="saveUserRole('${userId}',this)">Save</button>`;
            $cell.html(dropdown + saveBtn);
        }
    });
}
function deleteUser(userId) {
    if (confirm("Are you sure you want to delete this user?")) {
        $.ajax({
            url: '/AdminArea/Admin/Delete',
            type: 'POST',
            data: { id: userId },
            success: function (result) {
                if (result.success) {
                    alert(result.message);
                    loadDataTable();
                } else {
                    alert(result.message || "Failed to delete user.");
                }
            },
            error: function () {
                alert("Server error while deleting user.");
            }
        });
    }
}

function saveUserRole(userId, saveButton) {
    const $row = $(saveButton).closest('tr');
    const selectedRole = $row.find('.role-selector').val();
    $.ajax({
        url: '/AdminArea/Admin/EditRole',
        type: 'POST',
        data: {
            userId: userId,
            selectedRoles: [selectedRole]
        },
        success: function () {
            alert('Role Updated!');
            loadDataTable();
        },
        error: function () {
            alert('Failed to update Role!');
        }
    });
}