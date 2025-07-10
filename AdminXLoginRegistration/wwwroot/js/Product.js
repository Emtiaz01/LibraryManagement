$(document).ready(function () {
    $('#ProductTable').DataTable({
        ajax: {
            url: '/AdminArea/Product/GetProduct',
            type: 'GET',
            dataSrc: ''
        },
        columns: [
            { data: 'productName' },
            { data: 'productISBN'},
            { data: 'productAuthor'},
            {
                data: 'productPrice',
                render: $.fn.dataTable.render.number(',', '.', 2, '৳ ')
            },
            { data: 'productQuantity' },
            { data: 'category.categoryName'},
            {
                data: 'productId',
                render: function (data) {
                    return `
            <div class="w-75 btn-group" role="group">
                <a href="/AdminArea/Product/Upsert?id=${data}" class="btn btn-sm btn-primary m-1">
                    <i class="bi bi-pencil-square"></i> Edit
                </a>
                <button onclick="DeleteProduct(${data})" class="btn btn-sm btn-danger m-1">
                    <i class="bi bi-trash-fill"></i> Delete
                </button>
            </div>`;
                }
            }

        ]
    });
});

function DeleteProduct(id) {
    if (confirm("Are you sure you want to delete this product?")) {
        $.ajax({
            url: `/AdminArea/Product/Delete/${id}`,
            type: 'DELETE',
            success: function (result) {
                alert("Product deleted successfully.");
                $('#ProductTable').DataTable().ajax.reload();
            },
            error: function (xhr) {
                alert("Error deleting product: " + xhr.responseText);
            }
        });
    }
}
