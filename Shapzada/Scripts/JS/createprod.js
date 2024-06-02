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
            $('#priceError').text('');
        }

        if ($('#Quantity').val() === '') {
            $('#quantityError').text('Quantity is required.');
            isValid = false;
        } else {
            $('#quantityError').text('');
        }

        if ($('#Image').get(0).files.length === 0) {
            $('#imageError').text('Image is required.');
            isValid = false;
        } else {
            $('#imageError').text('');
        }

        return isValid;
    });
});