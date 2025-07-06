$(document).ready(function () {
    CategoryDataTable();
});

function CategoryDataTable() {
    $('#CategoryTable').DataTable({
        processing: true,
        serverSide: false,
        ajax: {
            url: '/AdminArea/Category/GetCategory',
            type: 'GET',
            dataSrc: ''
        },
        columns: [
            { data: 'categoryName' },
            { data: 'categoryDescription' },
            {
                data: 'categoryId',
                render: function (data) {
                    return `<div class="w-75 btn-group" role="group">
                                <a href="/AdminArea/Category/UpsertCategory?id=${data}" class="btn btn-sm btn-primary">
                                    <i class="bi bi-pencil-square"></i> Edit
                                </a>
                                <button onclick="DeleteCategory(${data})" class="btn btn-sm btn-danger">
                                    <i class="bi bi-trash-fill"></i> Delete
                                </button>
                            </div>`;
                }
            }
        ],
        destroy: true 
    });
}


function DeleteCategory(id) {
    if (confirm("Are you sure you want to delete this category?")) {
        $.ajax({
            type: "POST",
            url: '/AdminArea/Category/DeleteIT/' + id,
            success: function (response) {
                if (response.success) {
                    alert(response.message);
                    $('#CategoryTable').DataTable().ajax.reload(); 
                } else {
                    alert(response.message);
                }
            }
        });
    }
}
