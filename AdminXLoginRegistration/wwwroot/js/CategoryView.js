$(document).ready(function () {
    CategoryDataTable();
});

function CategoryDataTable() {
    $.ajax({
        url: '/Category/GetCategory',
        type: 'GET',
        dataType: 'json',
        success: OnSuccess
    });
}

function OnSuccess(response) {
    $('#CategoryTable').DataTable({
        bProcessing: true,
        bLengthChange: true,
        lengthMenu: [[5, 10, 25, -1], [5, 10, 25, "All"]],
        bFilter: true,
        bSort: true,
        bPaginate: true,
        destroy: true,
        data: response,
        columns: [
            { data: 'categoryName' },
            { data: 'categoryDescription' },
            {
                data: 'categoryId',
                "render": function (data) {
                    return `<div class="w-75 btn-group" role="group">
                                <a href="/Category/UpsertCategory?id=${data}" class="btn btn-sm btn-primary">
                                    <i class="bi bi-pencil-square"></i> Edit
                                </a>
                                <a href="/Category/Delete?id=${data}" class="btn btn-sm btn-danger">
                                    <i class="bi bi-trash-fill"></i> Delete
                                </a> 
                                </div>`

                }
            }
        ]
    });
}