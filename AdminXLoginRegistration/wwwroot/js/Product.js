$(document).ready(function () {
    $('#ProductTable').DataTable({
        ajax: {
            url: '/AdminArea/Product/GetProduct',
            type: 'GET',
            dataSrc: ''
        },
        columns: [
            { data: 'productName' },
            { data: 'description' },
            { data: 'productISBN'},
            { data: 'productAuthor'},
            {
                data: 'productPrice',
                render: $.fn.dataTable.render.number(',', '.', 2, '৳ ')
            },
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
