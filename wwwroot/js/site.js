// Data tables
$('#projectTable').DataTable({
    responsive: true,
    language: {
        searchPlaceholder: 'Search...',
        sSearch: '',
        lengthMenu: '_MENU_ items/page',
    },
    columns: [
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        { orderable: false }
    ]
});
$('#ticketTable').DataTable({
    responsive: true,
    language: {
        searchPlaceholder: 'Search...',
        sSearch: '',
        lengthMenu: '_MENU_ items/page',
    },
    columns: [
        null,
        null,
        null,
        null,
        null,
        null,
        { orderable: false }
    ]
});
$('#inviteTable').DataTable({
    responsive: true,
    language: {
        searchPlaceholder: 'Search...',
        sSearch: '',
        lengthMenu: '_MENU_ items/page',
    },
    columns: [
        null,
        null,
        null,
        null,
        null,
        null,
        { orderable: false }
    ]
});