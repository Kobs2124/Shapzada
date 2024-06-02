<script>
    $(function () {
        $('#searchButton').click(function () {
            // Get the search term
            var searchTerm = $('#searchTerm').val();

            // Redirect to the search action with the search term as a parameter
            window.location.href = '/Home/Search?searchTerm=' + searchTerm;
        });
        });
</script>