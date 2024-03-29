$(document).ready(function () {
    var MAX_INPUTTEXT_LENGTH  = 10000,
        LOCALSTORAGE_TEXT_KEY = 'udl-text',
        DEFAULT_TEXT          = 'Захворванне абумоўлена губчастым перараджэннем галаўны мозг галаўнога мозга. ' +
'У жывёл найбольш вылучаюць спангіяформную энцэфалапатыю буйной рагатай жывёлы т.зв. шаленства кароў ад мяса якіх адбываецца заражэнне людзей скрыты перыяд у людзей - гадоў.';

    var textOnChange = function () {
        let len = $('#text').val().length, len_txt = len.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ' ');
        $('#textLength').toggleClass('max-inputtext-length', MAX_INPUTTEXT_LENGTH < len).html('length of text: ' + len_txt + ' characters');
    };
    var getText = function ($text) {
        var text = trim_text($text.val().toString());
        if (is_text_empty(text)) {
            alert("Enter the text for processing.");
            $text.focus();
            return (null);
        }

        if (text.length > MAX_INPUTTEXT_LENGTH) {
            if (!confirm('Exceeded the recommended limit ' + MAX_INPUTTEXT_LENGTH + ' characters (on the ' + (text.length - MAX_INPUTTEXT_LENGTH) + ' characters).\r\nText will be truncated, continue?')) {
                return (null);
            }
            text = text.substr(0, MAX_INPUTTEXT_LENGTH);
            $text.val(text);
            $text.change();
        }
        return (text);
    };

    $("#text").focus(textOnChange).change(textOnChange).keydown(textOnChange).keyup(textOnChange).select(textOnChange).focus();

    (function () {
        function isGooglebot() { return (navigator.userAgent.toLowerCase().indexOf('googlebot/') !== -1); };
        if (isGooglebot())
            return;

        var text = localStorage.getItem(LOCALSTORAGE_TEXT_KEY);
        if (!text || !text.length) text = DEFAULT_TEXT;
        $('#text').val(text).focus();
    })();
    $('#resetText2Default').click(function () {
        $("#text").val('');
        setTimeout(() => $("#text").val(DEFAULT_TEXT).focus(), 100);
    });

    $('#processButton').click(function () {
        if($(this).hasClass('disabled')) return (false);

        var text = getText( $("#text") );
        if (!text) return (false);

        processing_start();
        if (text !== DEFAULT_TEXT) {
            localStorage.setItem(LOCALSTORAGE_TEXT_KEY, text);
        } else {
            localStorage.removeItem(LOCALSTORAGE_TEXT_KEY);
        }

        var model = {
            text: text
        };
        $.ajax({
            type       : 'POST',
            contentType: 'application/json',
            dataType   : 'json',
            url        : '/Process/Run',
            data       : JSON.stringify( model ),
            success: function (resp) {
                if (resp.err) {
                    processing_end();
                    $('.result-info').addClass('error').text(resp.err);
                } else {
                    $('.result-info').removeClass('error');

                    if (resp.langs && resp.langs.length) {
                        var trs = [], $div = $('<div>');
                        for (var i = 0, len = resp.langs.length; i < len; i++) {
                            var li = resp.langs[ i ];
                            trs.push( '<tr><td>' + $div.text( li.l ).html() + '</td><td>' + $div.text( li.n ).html() + '</td><td>' + $div.text( li.p ).html() + '%</td></tr>' );
                        }
                        
                        setTimeout(() => {
                            processing_end();
                            $('#processResult tbody').html( trs.join('') );
                            $('.result-info').text('').hide();
                        }, 100/*250*/);
                    } else {
                        processing_end();
                        $('.result-info').text('Language of text is not defined');
                    }
                }
            },
            error: function () {
                processing_end();
                $('.result-info').addClass('error').text('server error');
            }
        });
        
    });

    function processing_start() {
        $('#text').addClass('no-change').attr('readonly', 'readonly').attr('disabled', 'disabled');
        $('.result-info').show().removeClass('error').html('Processing... <label id="processingTickLabel"></label>');
        $('#processButton').addClass('disabled').attr('disabled', 'disabled');
        $('#processResult tbody').empty();
        processingTickCount = 1; setTimeout(processing_tick, 1000);
    };
    function processing_end() {
        $('#text').removeClass('no-change').removeAttr('readonly').removeAttr('disabled');
        $('.result-info').removeClass('error').text('');
        $('#processButton').removeClass('disabled').removeAttr('disabled').focus();
    };
    function trim_text(text) { return (text.replace(/(^\s+)|(\s+$)/g, "")); };
    function is_text_empty(text) { return (!trim_text(text)); };

    var processingTickCount,
        processing_tick = function () {
            var n2 = function (n) {
                n = n.toString();
                return ((n.length === 1) ? ('0' + n) : n);
            }
            var d = new Date(new Date(new Date(new Date().setHours(0)).setMinutes(0)).setSeconds(processingTickCount));
            var t = n2(d.getHours()) + ':' + n2(d.getMinutes()) + ':' + n2(d.getSeconds()); //d.toLocaleTimeString();
            var $s = $('#processingTickLabel');
            if ($s.length) {
                $s.text(t);
                processingTickCount++;
                setTimeout(processing_tick, 1000);
            } else {
                processingTickCount = 1;
            }
        };
});