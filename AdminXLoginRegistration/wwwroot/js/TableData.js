
$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    $.ajax({
        url: '/Admin/GetUser',
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
            { data: 'phoneNumber' },
            {
                data: 'userRoles',
                render: function (data, type, row) {
                    return data && data.length > 0 ? data.join(', ') : 'No Role';
                }
            },
            {
                data: 'id',
                "render": function (data) {
                    return `<div class="btn-group" role="group">
                                <a href="/Admin/EditRole?id=${data}" class="btn btn-sm btn-primary">
                                    <i class="bi bi-pencil-square"></i> Edit
                                </a>
                                <a asp-controller="Category" asp-action="Delete" asp-route-id="@user.Id" class="btn btn-sm btn-danger">
                                    <i class="bi bi-trash-fill"></i> Delete
                                </a>`
                }
            }
        ]
    });
}