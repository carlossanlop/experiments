// For details on configuring this project to bundle and minify static web assets:
// https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification

function GetAlert(message) {
    var str = '<div class="alert alert-warning alert-dismissible fade show" role="alert">';
    str += message;
    str += '<button type="button" class="close" data-dismiss="alert" aria-label="Close">';
    str += '<span aria-hidden="true">&times;</span>';
    str += '</button>';
    str += '</div>';
}

function GetPoints() {
    $.ajax({
        url: "/GetPoints",
        type: "GET",
        data: {
            "repositoryName": $("#RepoInput").val(),
            "from": $("#FromInput").val(),
            "to": $("#ToInput").val()
        },
        success: function (result) {
            AddData(result);
        },
        error: function (result) {
            var alert = GetAlert(result);
            $("#Alerts").html(alert);
        }
    });
}

function AddData(chart, label, data) {
    /*{
    labels: ["Red", "Blue", "Yellow", "Green", "Purple", "Orange"],
        datasets: [{
            label: "Passrate",
            data: [12, 19, 3, 5, 2, 3],
            backgroundColor: [
                "rgba(255, 99, 132, 0.2)",
                "rgba(54, 162, 235, 0.2)",
                "rgba(255, 206, 86, 0.2)",
                "rgba(75, 192, 192, 0.2)",
                "rgba(153, 102, 255, 0.2)",
                "rgba(255, 159, 64, 0.2)"
            ],
            borderColor: [
                "rgba(255,99,132,1)",
                "rgba(54, 162, 235, 1)",
                "rgba(255, 206, 86, 1)",
                "rgba(75, 192, 192, 1)",
                "rgba(153, 102, 255, 1)",
                "rgba(255, 159, 64, 1)"
            ],
            borderWidth: 1
        }]
    }*/
    chart.data.labels.push(label);
    chart.data.datasets.forEach((dataset) => {
        dataset.data.push(data);
    });
    chart.update();
}

