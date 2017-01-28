
$(document).ready(function () {
    var textOnChange = function () {
        var _len = $("#text").val().length; 
        var len = _len.toString().replace(/\B(?=(\d{3})+(?!\d))/g, " ");
        var $textLength = $("#textLength");
        $textLength.html("length of the text: " + len + " characters");
    };
    var getText = function( $text ) {
        var text = trim_text( $text.val().toString() );
        if (is_text_empty(text)) {
            alert("Enter the text to be processed.");
            $text.focus();
            return (null);
        }
        return (text);
    };

    $("#text").focus(textOnChange).change(textOnChange).keydown(textOnChange).keyup(textOnChange).select(textOnChange).focus();

    $('#mainPageContent').on('click', '#processButton', function () {
        if($(this).hasClass('disabled')) return (false);

        var text = getText( $("#text") );
        if (!text) return (false);   
        
        processing_start();

        $.ajax({
            type: "POST",
            url:  "RESTProcessHandler.ashx",
            data: {
                text: text
            },
            success: function (responce) {
                if (responce.err) {
                    processing_end();
                    $('.result-info').addClass('error').text(responce.err);
                } else {
                    if (responce.langs && responce.langs.length != 0) {
                        $('.result-info').removeClass('error').text('');
                        text = '';                            
                        for (var i = 0, len = responce.langs.length; i < len; i++) {
                            var li = responce.langs[i];
                            text += '<tr><td>' + li.l + '</td><td>' + li.n + '</td><td>' + li.p + '%</td></tr>';
                        }
                        $('#processResult tbody').html( text );
                        processing_end();
                    } else {
                        processing_end();
                        $('.result-info').text('language of text is not defined');
                    }
                }
            },
            error: function () {
                processing_end();
                $('.result-info').text('server error');
            }
        });
        
    });

    force_load_model();

    function processing_start(){
        $('#text').addClass('no-change').attr('readonly', 'readonly').attr('disabled', 'disabled');
        $('.result-info').removeClass('error').text('Processing...');
        $('#processButton').addClass('disabled');
        $('#processResult tbody').empty();
    };
    function processing_end(){
        $('#text').removeClass('no-change').removeAttr('readonly').removeAttr('disabled');
        $('.result-info').removeClass('error').text('');
        $('#processButton').removeClass('disabled');
    };
    function trim_text(text) {
        return (text.replace(/(^\s+)|(\s+$)/g, ""));
    };
    function is_text_empty(text) {
        return (text.replace(/(^\s+)|(\s+$)/g, "") == "");
    };
    function force_load_model() {
        $.ajax({
            type: "POST",
            url: "RESTProcessHandler.ashx",
            data: { text: "_dummy_" }
        });
    };
});
