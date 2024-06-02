$(document).ready(function () {
    $('#createAdminForm').submit(function (event) {
        event.preventDefault();

        var email = $('#Email').val();
        var password = $('#Password').val();

        if (!email || !password) {
            $('#message').html('<div class="alert alert-danger">Email and Password are required.</div>');
            return;
        }

        $.ajax({
            url: '/Home/CreateAdminProcess',
            type: 'POST',
            data: {
                admin_email: email,
                admin_pass: password
            },
            success: function (response) {
                if (response.success) {
                    $('#message').html('<div class="alert alert-success">' + response.message + '</div>');
                    $('#createAdminForm')[0].reset();
                } else {
                    $('#message').html('<div class="alert alert-danger">' + response.message + '</div>');
                }
            },
            error: function (xhr, status, error) {
                $('#message').html('<div class="alert alert-danger">An error occurred while processing your request.</div>');
                console.error(error);
            }
        });
    });
});
