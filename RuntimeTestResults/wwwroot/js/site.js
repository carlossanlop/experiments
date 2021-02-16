// For details on configuring this project to bundle and minify static web assets:
// https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification

// Generates and returns a Chart.js configuration dictionary with default values, including an extra list (points) where advanced line information is saved.
function GetEmptyOptions()
{
    var rgba = "rgba(255, 255, 255, 0.5)";

    var options = {
        type: 'line',
        data:
        {
            labels: [], // Put the chart bottom labels here
            points: [], // Put the line points here
            datasets: [] // Put the lines here
        },
        options: {
            scales: {
                yAxes: [{
                    gridLines: {
                        color: rgba
                    },
                    ticks: {
                        beginAtZero: true,
                        fontColor: 'white',
                        min: 0.0,
                        max: 100.0
                    }
                }],
                xAxes: [{
                    gridLines: {
                        color: rgba
                    },
                    ticks: {
                        fontColor: 'white'
                    }
                }]
            },
            legend: {
                labels: {
                    fontColor: 'white'
                }
            },
            pointLabels: {
                fontColor: 'white'
            }
        }
    };

    return options;
}

// Creates a new Chart.js inside the specified canvas.
function CreateChart(canvasElement)
{
    var options = GetEmptyOptions();
    var chart = new Chart(canvasElement, options);
    return chart;
}

// Returns the html text for a bootstrap alert div, embedding the specified message.
function GetAlertText(message)
{
    var str = '<div class="alert alert-warning alert-dismissible fade show" role="alert">';
    str += message;
    str += '<button type="button" class="close" data-dismiss="alert" aria-label="Close">';
    str += '<span aria-hidden="true">&times;</span>';
    str += '</button>';
    str += '</div>';
    return str;
}

// Inserts a bootstrap alert into the predefined alerts container, embedding the specified message.
function InsertAlert(message)
{
    var alert = GetAlertText(message);
    $("#Alerts").html($("#Alerts").html() + alert);
}

// Makes an ajax call to the specified url and GET data, expecting json and handling it with the specified success and error delegates.
function CallAjaxJson(url, data, success, error)
{
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

function AddChartData(jobs)
{
    var points = [];
    var labels = [];
    var datas  = [];
    var colors = [];
    var min = 100.0;
    var max = 0.0;

    $.each(jobs, function (i, job)
    {
        points.push(job);
        labels.push(job.FinishedShort);
        datas.push(job.Passrate);
        colors.push(job.Color);

        if (min > passrate)
        {
            min = passrate;
        }
        if (max < passrate)
        {
            max = passrate;
        }

    });

    if (max == 0.0)
    {
        max = 100.0;
    }

    if (min == 100.0)
    {
        min = 0.0;
    }

    if (max == min)
    {
        max = 100.0;
        min = 0.0;
    }

    var dataset = {
        label: "Jobs",
        data: datas,
        borderColor: colors,
        backgroundColor: colors,
        fill: false,
        borderWidth: 1
    };

    var options = GetEmptyOptions();

    options.scales.yAxes[0].ticks.min = min;
    options.scales.yAxes[0].ticks.max = max;

    chart.data.points = points;
    chart.data.labels = labels;
    chart.data.datasets = [dataset];
    chart.options = options;
    chart.onclick = function (evt) { OnClickChartPoint(evt, config); };
    chart.update();
}

/// Determines the actions to be executed when the user clicks on an clickable point of a line in the chart.
function OnClickChartPoint(evt, config)
{
    var evtElements = chart.getElementAtEvent(evt);

    // When the user clicks on the chart, we only act when there is a clickable point
    if (evtElements !== undefined &&
        evtElements !== null &&
        evtElements instanceof Array &&
        evtElements.length > 0)
    {
        var evtPoint = evtElements[0];

        // var lineIndex = evtPoint._datasetIndex;
        // var line = config.data.datasets[lineIndex];
        var pointIndex = evtPoint._index;
        var point = config.data.points[pointIndex];

        ShowModalDialog(title, point);

        // fullChartLines is a custom object that was added to the config data object to be used in Sisyphus.
        // It's an array of the same length as the DataSets array, and it contains all the lines in the chart, defined as JSONified ChartLine objects.
        //var chartLine = config.data.points[datasetIndex];
        //var selectedChartPoint = chartLine.ChartPoints[activePointIndex];
        //var selectedJob = selectedChartPoint.Jobs[0];
        //var bucketFriendlyName = chartLine.QuerySelections.Bucket.FriendlyName;
        //var jobIDs = selectedJob.JobID;

        //if (jobIDs !== -1)
        //{
        //    var extraFriendlyTitle = GetChartTitleExtra(jobIDs, selectedJob.BaselineCheckpoint, selectedJob.OloopCheckpoint, selectedJob.OloopIteration);

        //    // Open the modal, give it a title, but the body is the loading gif
        //    ShowModalWindow("<small>Details of the " + FriendlyTitle + extraFriendlyTitle + "</small>", GifLoading);

        //    // The actual body with the point details will be requested via ajax here
        //    GetPointDetails(selectedJob.JobID);

        //    // Enable sorting for the internal dialog table
        //    // TODO: The CSS is not working yet
        //    //$("#" + PathResultTableID).DataTable();

        //    console.log(selectedChartPoint);
        //}
    }
}

function GetPoints()
{
    TmpChartButton();
}

function GetRepoJobs()
{
    var data = {
        "repositoryName": $("#RepoInput").val(),
        "from": $("#FromInput").val(),
        "to": $("#ToInput").val()
    };

    var success = function (jobs) {
        AddChartData(jobs);
    };

    var error = function (result) {
        InsertAlert(result.status + ": " + result.statusText);
    };

    CallAjaxJson(urlGetRepoJobs, data, success, error);
}

// Testing method that returns jobs. Delete when ready.
function TmpChartButton()
{
    var jobs = [
        {
            "FinishedShort": "2021/02/15 09:00 AM",
            "TestsPass": 2,
            "TestsFail": 8,
            "Passrate": 20.0,
            "Color": "rgba(255, 0, 0, 0.5)"
        },
        {
            "FinishedShort": "2021/02/15 09:30 AM",
            "TestsPass": 7,
            "TestsFail": 3,
            "Passrate": 70.0,
            "Color": "rgba(255, 0, 0, 0.5)"
        },
        {
            "FinishedShort": "2021/02/15 10:00 AM",
            "TestsPass": 3,
            "TestsFail": 7,
            "Passrate": 30.0,
            "Color": "rgba(255, 0, 0, 0.5)"
        },
        {
            "FinishedShort": "2021/02/15 10:30 AM",
            "TestsPass": 9,
            "TestsFail": 1,
            "Passrate": 90.0,
            "Color": "rgba(255, 0, 0, 0.5)"
        }
    ];
    AddChartData(jobs);
}

function ShowModalDialog(title, body)
{
    $("#ModalDialogTitle").html(title);
    $("#ModalDialogBody").html(body);
    $("#ModalDialog").modal("toggle");
}