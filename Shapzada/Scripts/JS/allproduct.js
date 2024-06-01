$(document).ready(function () {
    $(".add-to-cart-button").click(function () {
        var productId = $(this).closest(".product-card").data("product-id");

        $.ajax({
            url: "/Home/AddToCart",
            type: "POST",
            data: { productId: productId },
            success: function () {
                // Optional: Show a success message or update the UI
                alert("Product added to cart!");
            },
            error: function () {
                // Optional: Show an error message or handle the error
                alert("Failed to add product to cart.");
            }
        });
    });
});
