// For details on configuring this project to bundle and minify static web assets:
// https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification

function CreateChart() {

    var ctx = document.getElementById("ResultsChart");

    var options = {
        type: "line",
        options: {
            scales: {
                yAxes: [{
                    ticks: {
                        beginAtZero: true
                    }
                }]
            }
        }
    };

    var chart = new Chart(ctx, options);

    return chart;
}

function GetAlert(message) {
    var str = '<div class="alert alert-warning alert-dismissible fade show" role="alert">';
    str += message;
    str += '<button type="button" class="close" data-dismiss="alert" aria-label="Close">';
    str += '<span aria-hidden="true">&times;</span>';
    str += '</button>';
    str += '</div>';
    return str;
}

function AddAlert(str) {
    var alert = GetAlert(str);
    $("#Alerts").html($("#Alerts").html() + alert);
}

function CallAjax(url, data, success, error) {
    $.ajax({
        url: url,
        type: "GET",
        dataType: "json",
        contentType: "application/json; charset=utf-8",
        data: data,
        success: success,
        error: error
    });
}

function AddChartData(jobs) {

    var labels = [];
    var datas = [];
    var backgroundColors = [];
    var borderColors = [];

    $.each(jobs, function (i, job) {

        labels.push(job.FinishedShort);

        var passrate = parseFloat(job.TestsPass * 100.0 / (job.TestsFail + job.TestsPass));
        datas.push(passrate);

        backgroundColors.push("rgba(255, 0, 0, 0)");
        borderColors.push("rgba(200, 0, 0, 0)");
    });

    var dataset = {
        label: "Jobs",
        data: datas,
        backgroundColor: backgroundColors,
        borderColor: borderColors,
        borderWidth: 1
    };

    chart.data.labels = labels;
    chart.data.datasets = dataset;
    chart.update();

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
    //chart.data.labels.push(label);
    //chart.data.datasets.forEach((dataset) => {
    //    dataset.data.push(data);
    //});
    //chart.update();
}

