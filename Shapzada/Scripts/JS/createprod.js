$(document).ready(function () {
    $('#createProductForm').submit(function (e) {
        var isValid = true;

        // Basic validation for required fields
        if ($('#Name').val() === '') {
            $('#nameError').text('Name is required.');
            isValid = false;
        } else {
            $('#nameError').text('');
        }

        if ($('#Price').val() === '') {
            $('#priceError').text('Price is required.');
            isValid = false;
        } else {
            var price = parseFloat($('#Price').val());
            if (price < 0) {
                $('#priceError').text('Price cannot be negative.');
                isValid = false;
            } else {
                $('#priceError').text('');
            }
        }

        if ($('#Quantity').val() === '') {
            $('#quantityError').text('Quantity is required.');
            isValid = false;
        } else {
            var quantity = parseInt($('#Quantity').val());
            if (quantity < 0) {
                $('#quantityError').text('Quantity cannot be negative.');
                isValid = false;
            } else {
                $('#quantityError').text('');
            }
        }

        if ($('#Image').get(0).files.length === 0) {
            $('#imageError').text('Image is required.');
            isValid = false;
        } else {
            $('#imageError').text('');
        }

        if (!isValid) {
            e.preventDefault(); // Prevent form submission
        }

        return isValid;
    });

    // Clear error messages on input change
    $('#Name').on('input', function () {
        $('#nameError').text('');
    });

    $('#Price').on('input', function () {
        $('#priceError').text('');
    });

    $('#Quantity').on('input', function () {
        $('#quantityError').text('');
    });

    $('#Image').on('change', function () {
        $('#imageError').text('');
    });
});
