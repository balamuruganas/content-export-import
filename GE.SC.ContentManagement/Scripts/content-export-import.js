$(document).ready(function () {
    $('#drpTemplates').change(function () {
        $("#TemplateFields").removeAttr("disabled");
        $.ajax({
            url: '/api/sc/GetTemplateFields',
            type: 'POST',
            data: { id: $('#drpTemplates').val() },
            cache: false,
            success: function (data) {
                var lstItems = [];
                for (var i = 0; i < data.length; i++) {
                    lstItems.push("<option value=" + data[i].Name + ">" + data[i].Name + "</option>");
                }
                $('#TemplateFields').empty();
                $('#TemplateFields').append(lstItems);
                $("#templatefields").parents('.form-field').removeClass("template-fields");
                $("#TemplateFields").ultraselect({ noneSelected: "Select Fields" });
            }
        });
    });
    $("#IncludeChildren").click(function () {
        if (this.checked) {
            $("#drpTemplates").removeAttr("disabled");
        }
        else {
            $("#drpTemplates").attr("disabled", true);
        }
    });
    $("#export").click(function () {
        var data = {
            "SelectedLanguage": $("#drpLanguages").val(),
            "RootItem": $("#myItem").val(),
            "IncludeChildren": $("#IncludeChildren").val(),
            "SelectedTemplate": $("#drpTemplates").val(),
            "SelectedFields": $("#TemplateFields").val()
        };

        $.ajax({
            type: "POST",
            data: JSON.stringify(data),
            url: '/api/sc/export',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
        }).done(function (data) {
            if (data.message != "") {
                $(".statusMessage").text(data.message);
            }
            if (data.fileName != "") {
                window.location.href = "/api/sc/export/download?fileName=" + data.fileName;
            }

        });
    });
    $("#import").click(function () {
        var selectedLanguage = $("#drpLanguages").val();
        var displayName = $("#chkBoxDisplayName").val();
        var formData = new FormData();
        var totalFiles = document.getElementById("dataFile").files.length;

        for (var i = 0; i < totalFiles; i++) {
            var file = document.getElementById("dataFile").files[i];

            formData.append("FileUpload", file);
        }

        $.ajax({
            type: 'POST',
            url: '/api/sitecore/geimport/contentimport?language=' + selectedLanguage + '&displayName=' + displayName,
            data: formData,
            dataType: 'json',
            contentType: false,
            processData: false,
            success: function (data) {
                if (data.message != "") {
                    $(".statusMessage").text(data.message);
                }
            },
            error: function (data) {
                if (data.message != "") {
                    $(".statusMessage").text(data.message);
                }
            }
        });
    });
});