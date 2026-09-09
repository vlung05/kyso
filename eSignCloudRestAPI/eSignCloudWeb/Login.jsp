










<!DOCTYPE html>

<html>
    <head>
        <title></title>
        <meta http-equiv="Content-Type" content="text/html; charset=UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <link href="js/bootstrap.min.css" rel="stylesheet">
        <link href="font-awesome/css/font-awesome.min.css" rel="stylesheet" type="text/css">
        <script nonce="pQg9RVb53Sc0fUywUG1nDQ==">
            console.log("Script loaded with nonce: pQg9RVb53Sc0fUywUG1nDQ==");
        </script>
        <script type="text/javascript" src="js/jquery.js" nonce="pQg9RVb53Sc0fUywUG1nDQ=="></script>
        <script nonce="pQg9RVb53Sc0fUywUG1nDQ==">
            $(document).ready(function () {
                if (localStorage.getItem("VN") !== null && localStorage.getItem("VN") !== "null") {
                    
                }
                else
                {
                    localStorage.setItem("VN", "1");
                }
                var sLangugeCurrent = localStorage.getItem("VN");
                //BaoTG
                const show_hide = localStorage.getItem("gridColPrefs");
                localStorage.clear();
                localStorage.setItem("VN", sLangugeCurrent);
                if (show_hide) localStorage.setItem("gridColPrefs", show_hide);
                //BaoTG
            });
        </script>
        <script src="js/Language.js" nonce="pQg9RVb53Sc0fUywUG1nDQ=="></script>
        <script src="js/process_javajs.js" nonce="pQg9RVb53Sc0fUywUG1nDQ=="></script>
        <link rel="stylesheet" href="js/cssLogin_1.css">
        <script src="js/sweetalert-dev.js" nonce="pQg9RVb53Sc0fUywUG1nDQ=="></script>
        <link rel="stylesheet" href="js/sweetalert.css"/>
        <script type="text/javascript" src="Css/GlobalAlert.js" nonce="pQg9RVb53Sc0fUywUG1nDQ=="></script>
        <script src="style/jquery.min.js" nonce="pQg9RVb53Sc0fUywUG1nDQ=="></script>
        <script nonce="pQg9RVb53Sc0fUywUG1nDQ==">
        
        </script>
        <script nonce="pQg9RVb53Sc0fUywUG1nDQ==">
            changeFavicon("");
            $(document).ready(function () {
                document.title = TitleLoginPage;
                var image = document.getElementById('idLogoPageHeader');
                image.src = LinkLogoPage;
                if('0' === "1") {
                    image.src = "Images/Logo_Minvoice_210.png";
                }
                $(".loading-gif").hide();
                $("#sUserName").focus(function () {
                    if ($("#sUserName").val() === global_fm_Username)
                    {
                        $("#sUserName").val('');
                    }
                });
                $("#sUserName").blur(function () {
                    if ($("#sUserName").val() === "") {
                        $("#sUserName").val(global_fm_Username);
                    }
                });

                $("#sPwd").focus(function () {
                    $('#sPwd').attr("placeholder", "");
                });
                $("#sPwd").blur(function () {
                    if ($("#sPwd").val() === "")
                    {
                        $('#sPwd').attr("placeholder", global_fm_Password);
                    }
                });
                $("#idCaptchaText").focus(function () {
                    if ($("#idCaptchaText").val() === login_fm_captcha)
                    {
                        $("#idCaptchaText").val('');
                    }
                });
                $("#idCaptchaText").blur(function () {
                    if ($("#idCaptchaText").val() === "")
                    {
                        $("#idCaptchaText").val(login_fm_captcha);
                    }
                });
            });

            $(document).ready(function () {
                $('#sPwd').keydown(function (event) {
                    if (event.keyCode === 13) {
                        var isCA = '18';
                        if(isCA !== JS_IS_WHICH_ABOUT_CA_ICA){
                            loginPage(document.getElementById("CsrfToken").value);
                            return false;
                        } else {
                            loginPageNoCaptcha(document.getElementById("CsrfToken").value);
                            return false;
                        }
                    }
                });
                $('#idCaptchaText').keydown(function (event) {
                    if (event.keyCode === 13) {
                        var isCA = '18';
                        if(isCA !== JS_IS_WHICH_ABOUT_CA_ICA){
                            loginPage(document.getElementById("CsrfToken").value);
                            return false;
                        } else {
                            loginPageNoCaptcha(document.getElementById("CsrfToken").value);
                            return false;
                        }
                    }
                });
                $('#sUserName').keydown(function (event) {
                    if (event.keyCode === 13) {
                        var isCA = '18';
                        if(isCA !== JS_IS_WHICH_ABOUT_CA_ICA){
                            if ($('#sPwd').val() !== '' && $('#sUserName').val() !== '' && $('#idCaptchaText').val() !== '')
                            {
                                loginPage(document.getElementById("CsrfToken").value);
                                return false;
                            }
                            else
                            {
                                funErrorLoginAlert(global_req_all);
                                return false;
                            }
                        } else {
                            if ($('#sPwd').val() !== '' && $('#sUserName').val() !== '')
                            {
                                loginPageNoCaptcha(document.getElementById("CsrfToken").value);
                                return false;
                            }
                            else
                            {
                                funErrorLoginAlert(global_req_all);
                                return false;
                            }
                            
                        }
                        
                    }
                });
            });
            function fogetPage()
            {
                window.location = "Forgot.jsp";
            }
            function loginPage(idCSRF)
            {
                var name = document.loginform.sUserName;
                var pass = document.loginform.sPwd.value;
                var capchas = document.loginform.CaptchaText.value;
                if (!JSCheckEmptyField(name.value) || sSpace(name.value) === global_fm_Username)
                {
                    name.focus();
                    funErrorLoginAlert(policy_req_empty + global_fm_Username);
                    return false;
                }
                else if (!JSCheckEmptyField(pass))
                {
                    funErrorLoginAlert(policy_req_empty + global_fm_Password);
                    return false;
                }
                if (!JSCheckEmptyField(capchas))
                {
                    funErrorLoginAlert(policy_req_empty + login_fm_captcha);
                    return false;
                }
                else
                {
                    var s1 = document.getElementById("idCode").value;
                    if (trim(s1) !== trim(capchas))
                    {
                        LoadCaptcha();
                        funErrorLoginAlert(login_req_captcha);
                        return false;
                    }
                }
                //***if (name.value !== "" && pass !== "" && capchas !== login_fm_captcha && capchas !== "")
                if (name.value !== "" && pass !== "" && capchas !== login_fm_captcha && capchas !== "")
                {
                    if (name.value !== global_fm_Username)
                    {
                        var s1 = $("#idCode").val();
                        if (s1 !== capchas)
                        {
                            LoadCaptcha();
                            swal("", login_req_captcha, "error");
                            $("#idCaptchaText").focus();
                        }
                        else
                        {
                            $('body').append('<div id="over"></div>');
                            $(".loading-gif").show();
                            var s = localStorage.getItem("VN");
                            $.ajax({
                                type: "POST",
                                url: "LoginCommon",
                                error: function (jqXHR, textStatus) {
                                    if (textStatus === 'timeout')
                                    {
                                        $(".loading-gif").hide();
                                        $('#over').remove();
                                        swal({
                                            title: "", text: login_error_timeout, imageUrl: "Images/icon_error.png", imageSize: "45x45"
                                        });
                                    }
                                    else {
                                        $(".loading-gif").hide();
                                        $('#over').remove();
                                        swal({
                                            title: "", text: login_error_exception, imageUrl: "Images/icon_error.png", imageSize: "45x45"
                                        });
                                    }
                                },
                                data: {
                                    idParam: 'loginpage',
                                    sUserName: name.value,
                                    sPwd: pass,
                                    sCaptcha: capchas,
                                    svn: s,
                                    CsrfToken: idCSRF
                                },
                                cache: false,
                                success: function (html) {
                                    var arr = sSpace(html).split('#');
                                    if (arr[0] === "0") {
                                        if (arr[3] === "1") {
                                            
                                            localStorage.setItem("HasLoginLog", "1");
                                            localStorage.setItem("localStoreUserName_RoudRB", name.value);
                                            localStorage.setItem("localStoreSessKey_RoudRB", arr[2]);
                                            document.getElementById('loginChange').value=arr[1];
                                            document.getElementById('idViewOTP').style.display = '';
                                            document.getElementById('idViewLogin').style.display = 'none';
                                            document.getElementById("btnResend").disabled = true;
                                            startTimer();
                                            
                                        } else {
                                            localStorage.setItem("HasLoginLog", "1");
                                            localStorage.setItem("localStoreUserName_RoudRB", name.value);
                                            localStorage.setItem("localStoreSessKey_RoudRB", arr[2]);

                                            if (arr[1] === "1")
                                            {
                                                window.location = "Admin/LoginChange.jsp";
                                            }
                                            else
                                            {
                                                window.location = "Admin/Home.jsp";
                                            }
                                        }
                                    }
                                    else if (arr[0] === JS_EX_CSRF) {
                                        swal({title: "", text: CSRF_Mess, imageUrl: "Images/icon_error.png", imageSize: "45x45"},
                                        function () {
                                            window.location = "Modules/Logout.jsp";
                                        });
                                    }
                                    else if (arr[0] === "CAPTCHA") {
                                        $("#idCaptchaText").focus();
                                        funErrorLoginAlert(login_req_captcha);
                                    }
                                    else if (arr[0] === JS_EX_ERROR) {
                                        funErrorLoginAlert(global_errorsql);
                                    }
                                    else if (arr[0] === "1") {
                                        funErrorLoginAlert(login_error_lock);
                                    }
                                    else if (arr[0] === "2") {
                                        funErrorLoginAlert(login_error_incorrec);
                                    }
                                    else if (arr[0] === "3") {
                                        funErrorLoginAlert(login_error_incorrec);
                                    }
                                    else if (arr[0] === "4") {
                                        funErrorLoginAlert(login_error_inactive);
                                    }
                                    else if (arr[0] === "98") {
                                        localStorage.setItem("HasLoginLog", "1");
                                        localStorage.setItem("localStoreUserName_RoudRB", name.value);
                                        localStorage.setItem("localStoreSessKey_RoudRB", arr[2]);
                                        swal({
                                            title: "", text: global_pass_weak_change, imageUrl: "Images/icon_warning.png", imageSize: "45x45",
                                            allowOutsideClick: false, allowEscapeKey: false},
                                        function () {
                                            window.location = "Admin/LoginChange.jsp";
                                        });
//                                        funErrorAlertLocation(global_pass_weak_change,"Admin/LoginChange.jsp");
                                    }    
                                    else
                                    {
                                        if (arr[0] === "5") {
                                            funErrorLoginAlert(login_error_incorrec);
                                        }else{
                                            funErrorLoginAlert(global_errorsql);
                                        }
                                    }
                                    LoadCaptcha();
                                    $(".loading-gif").hide();
                                    $('#over').remove();
                                },
                                timeout: 100000
                            });
                            return false;
                        }
                    }
                    else
                    {
                        funErrorLoginAlert(global_req_all);
                    }
                }
                else
                {
                    funErrorLoginAlert(global_req_all);
                }
            }
            function loginPageNoCaptcha(idCSRF)
            {
                var name = document.loginform.sUserName;
                var pass = document.loginform.sPwd.value;
                if (!JSCheckEmptyField(name.value) || sSpace(name.value) === global_fm_Username)
                {
                    name.focus();
                    funErrorLoginAlert(policy_req_empty + global_fm_Username);
                    return false;
                } else {
                    if (!JSCheckEmptyField(pass))
                    {
                        funErrorLoginAlert(policy_req_empty + global_fm_Password);
                        return false;
                    }
                }
                if (name.value !== "" && pass !== "")
                {
                    if (name.value !== global_fm_Username)
                    {
                        $('body').append('<div id="over"></div>');
                        $(".loading-gif").show();
                        var s = localStorage.getItem("VN");
                        $.ajax({
                            type: "POST",
                            url: "LoginCommon",
                            error: function (jqXHR, textStatus) {
                                if (textStatus === 'timeout')
                                {
                                    $(".loading-gif").hide();
                                    $('#over').remove();
                                    swal({
                                        title: "", text: login_error_timeout, imageUrl: "Images/icon_error.png", imageSize: "45x45"
                                    });
                                }
                                else {
                                    $(".loading-gif").hide();
                                    $('#over').remove();
                                    swal({
                                        title: "", text: login_error_exception, imageUrl: "Images/icon_error.png", imageSize: "45x45"
                                    });
                                }
                            },
                            data: {
                                idParam: 'loginpage',
                                sUserName: name.value,
                                sPwd: pass,
                                sCaptcha: "",
                                svn: s,
                                CsrfToken: idCSRF
                            },
                            cache: false,
                            success: function (html) {
                                var arr = sSpace(html).split('#');
                                console.log("abc: "+ sSpace(html));
                                console.log("ar: "+ arr[0]);
                                if (arr[0] === "0") {
                                    localStorage.setItem("HasLoginLog", "1");
                                    localStorage.setItem("localStoreUserName_RoudRB", name.value);
                                    localStorage.setItem("localStoreSessKey_RoudRB", arr[2]);
                                    if (arr[1] === "1")
                                    {
                                        window.location = "Admin/LoginChange.jsp";
                                    } else if (arr[1] === "2")
                                    {
                                        checkRedrectHome("../Certificate/RegisterList.jsp");
//                                        window.location = "Certificate/RegisterList.jsp";
                                    }
                                    else
                                    {
                                        window.location = "Admin/Home.jsp";
                                    }
                                }
                                else if (arr[0] === JS_EX_CSRF) {
                                    swal({title: "", text: CSRF_Mess, imageUrl: "Images/icon_error.png", imageSize: "45x45"},
                                    function () {
                                        window.location = "Modules/Logout.jsp";
                                    });
                                }
                                else if (arr[0] === JS_EX_ERROR) {
                                    funErrorLoginAlert(global_errorsql);
                                }
                                else if (arr[0] === "1") {
                                    funErrorLoginAlert(login_error_lock);
                                }
                                else if (arr[0] === "2") {
                                    funErrorLoginAlert(login_error_incorrec);
                                }
                                else if (arr[0] === "3") {
                                    funErrorLoginAlert(login_error_incorrec);
                                }
                                else if (arr[0] === "4") {
                                    funErrorLoginAlert(login_error_inactive);
                                }
                                else if (arr[0] === "5") {
                                    funErrorLoginAlert(login_error_incorrec);
                                }
                                else
                                {
                                    funErrorLoginAlert(global_errorsql);
                                }
                                $(".loading-gif").hide();
                                $('#over').remove();
                            },
                            timeout: 100000
                        });
                        return false;
                    }
                    else
                    {
                        funErrorLoginAlert(global_req_all);
                    }
                }
                else
                {
                    funErrorLoginAlert(global_req_all);
                }
            }
            
            function checkOTPEmail(idCSRF)
            {
                var OTP = document.loginform.sOTP;
                if (!JSCheckEmptyField(OTP.value) || sSpace(OTP.value) === global_fm_OTP)
                {
                    OTP.focus();
                    funErrorLoginAlert(policy_req_empty + global_fm_OTP);
                    return false;
                }
                if (OTP.value !== "")
                {
                    if (OTP.value !== global_fm_OTP)
                    {
                        $('body').append('<div id="over"></div>');
                        $(".loading-gif").show();
                        $.ajax({
                            type: "POST",
                            url: "LoginCommon",
                            data: {
                                idParam: 'otpemailauthen',
                                OTP: OTP.value,
                                CsrfToken: idCSRF
                            },
                            cache: false,
                            success: function (html) {
                                var arr = sSpace(html).split('#');
                                if (arr[0] === "0") {
                                    var sLoginChange= document.getElementById("loginChange").value;
                                    if (sLoginChange === "1")
                                    {
                                        window.location = "Admin/LoginChange.jsp";
                                    } 
                                    else
                                    {
                                        window.location = "Admin/Home.jsp";
                                    }
                                }
                                else if (arr[0] === JS_EX_CSRF) {
                                    swal({title: "", text: CSRF_Mess, imageUrl: "Images/icon_error.png", imageSize: "45x45"},
                                    function () {
                                        window.location = "Modules/Logout.jsp";
                                    });
                                } else if (arr[0] === JS_EX_LOGIN)
                                {
                                    swal({title: "", text: global_alert_login, imageUrl: "Images/icon_error.png", imageSize: "45x45"},
                                    function () {
                                        window.location = "Modules/Logout.jsp";
                                    });
                                }
                                else if (arr[0] === "1") {
                                    funErrorLoginAlert(global_errorsql);
                                }
                                else if (arr[0] === "4001") {
                                    swal({title: "", text: login_error_lock, imageUrl: "Images/icon_error.png", imageSize: "45x45",
                                        allowOutsideClick: false, allowEscapeKey: false},
                                    function () {
                                        window.location = "Modules/Logout.jsp";
                                    });
                                }
                                else
                                {
                                    funErrorLoginAlert(arr[1]);
                                }
                                $(".loading-gif").hide();
                                $('#over').remove();
                            },
                            timeout: 100000
                        });
                        return false;
                    }
                    else
                    {
                        funErrorLoginAlert(global_req_all);
                    }
                }
                else
                {
                    funErrorLoginAlert(global_req_all);
                }
            }
            function resendOTPEmail(idCSRF)
            {
                $('body').append('<div id="over"></div>');
                $(".loading-gif").show();
                document.getElementById("btnResend").disabled = true;
                $.ajax({
                    type: "POST",
                    url: "LoginCommon",
                    data: {
                        idParam: 'resendotpemailauthen',
                        CsrfToken: idCSRF
                    },
                    cache: false,
                    success: function (html) {
                        var arr = sSpace(html).split('#');
                        if (arr[0] === "0") {
                            startTimer();
                            swal({
                                title: "", text: sendmailotp_success, imageUrl: "Images/success.png", imageSize: "45x45"
                            });
//                            funSuccNoLoad(sendmailotp_success)
                        }
                        else if (arr[0] === JS_EX_CSRF) {
                            swal({title: "", text: CSRF_Mess, imageUrl: "Images/icon_error.png", imageSize: "45x45"},
                            function () {
                                window.location = "Modules/Logout.jsp";
                            });
                        } else if (arr[0] === JS_EX_LOGIN)
                        {
                            swal({title: "", text: global_alert_login, imageUrl: "Images/icon_error.png", imageSize: "45x45"},
                            function () {
                                window.location = "Modules/Logout.jsp";
                            });
                        }
                        else if (arr[0] === "1") {
                            document.getElementById("btnResend").disabled = true;
                            funErrorLoginAlert(sendmailmotp_error);
                        }
                        else
                        {
                            document.getElementById("btnResend").disabled = true;
                            funErrorLoginAlert(global_errorsql);
                        }
                        $(".loading-gif").hide();
                        $('#over').remove();
                    },
                    timeout: 100000
                });
                return false;
            }
            function checkRedrectHome(pageCheck){
                $.ajax({
                    type: "post",
                    url: "SomeCommon",
                    data: {
                        idParam: 'checkredrecthome',
                        pageCheck: pageCheck
                    },
                    cache: false,
                    success: function (html)
                    {
                        var arr = sSpace(html).split('#');
                        if (arr[1] === "1") {
                            window.location = "Certificate/RegisterList.jsp";
                        }
                        else
                        {
                            window.location = "Admin/Home.jsp";
                        }
                    }
                });
            }
            function backPage()
            {
                window.location = "../Modules/Logout.jsp";
            }
            function loginSSLPage(idCSRF)
            {
                var s = localStorage.getItem("VN");
                LoadCaptcha();
                $.ajax({
                    type: "GET",
                    url: "GetCertSSL",
                    cache: false,
                    success: function (htmlSSL) {
                       var vResult = sSpace(htmlSSL).split('#');
                       if(vResult[0] === "0")
                       {
                           $('body').append('<div id="over"></div>');
                           $(".loading-gif").show();
                           $.ajax({
                                type: "POST",
                                url: "LoginCommon",
                                error: function (jqXHR, textStatus) {
                                    if (textStatus === 'timeout')
                                    {
                                        $(".loading-gif").hide();
                                        $('#over').remove();
                                        swal({
                                            title: "", text: login_error_timeout, imageUrl: "Images/icon_error.png", imageSize: "45x45"
                                        });
                                    }
                                    else {
                                        $(".loading-gif").hide();
                                        $('#over').remove();
                                        swal({
                                            title: "", text: login_error_exception, imageUrl: "Images/icon_error.png", imageSize: "45x45"
                                        });
                                    }
                                },
                                data: {
                                    idParam: 'loginssl',
                                    svn: s,
                                    CsrfToken: idCSRF
                                },
                                cache: false,
                                success: function (html) {
                                    var arr = sSpace(html).split('#');
                                    if (arr[0] === "0") {
                                        localStorage.setItem("HasLoginLog", "1");
                                        window.location = "Admin/Home.jsp";
                                    }
                                    else if (arr[0] === JS_EX_CSRF) {
                                        swal({title: "", text: CSRF_Mess, imageUrl: "Images/icon_error.png", imageSize: "45x45"},
                                        function () {
                                            window.location = "Modules/Logout.jsp";
                                        });
                                    }
                                    else if (arr[0] === JS_EX_ERROR) {
                                        funErrorLoginAlert(global_errorsql);
                                    }
                                    else if (arr[0] === "1") {
                                        funErrorLoginAlert(login_error_lock);
                                    }
                                    else if (arr[0] === "2") {
                                        funErrorLoginAlert(login_error_incorrec);
                                    }
                                    else if (arr[0] === "3") {
                                        funErrorLoginAlert(login_error_incorrec);
                                    }
                                    else if (arr[0] === "4") {
                                        funErrorLoginAlert(login_error_inactive);
                                    }
                                    else if (arr[0] === "5") {
                                        funErrorLoginAlert(login_error_token_ssl);
                                    } else if(arr[0] === JS_EX_ERRORCTS) {
                                        funErrorLoginAlert(global_error_cert_compare_ca);
                                    }
                                    else
                                    {
                                        funErrorLoginAlert(global_errorsql);
                                    }
                                    $(".loading-gif").hide();
                                    $('#over').remove();
                                },
                                timeout: 100000
                            });
                            return false;
                       } else if(vResult[0] === JS_EX_NO_CERTCHAN) {
                           funErrorLoginAlert(global_error_chain_cert);
                       } else if(vResult[0] === JS_EX_ERRORCTS) {
                           funErrorLoginAlert(global_error_cert_compare_ca);
                       } else {
                           funErrorLoginAlert(global_errorsql);
                       }
                    },
                    timeout: 100000
                });
                return false;
            }
        </script>
        <script nonce="pQg9RVb53Sc0fUywUG1nDQ==">
            function LoadCaptcha() {
                $.ajax({
                    type: "post",
                    url: "SomeCommon",
                    data: {
                        idParam: 'generatecaptcha'
                    },
                    cache: false,
                    success: function (html)
                    {
                        var arr = sSpace(html).split('#');
                        if (arr[1] !== "")
                        {
                            $("#idCode").val(arr[0]);
                            $("#idImageCap").attr("src", "data:image/jpg;base64," + sSpace(html).replace(arr[0] + "#", ""));
                        }
                        else
                        {
                            funErrorLoginAlert(arr[0]);
                        }
                    }
                });
            }
        </script>
    </head>
    <body>
        
        <div style="width: 100%; text-align: center; position: fixed;z-index: 1000;top: 0; padding-top: 300px;
             left: 0; height: 100%;" class="loading-gif">
            <img src="Images/ajax-loader1.gif" alt="Please wait..." /> 
        </div>
        
        <div id="header-two">
            <div class="header-two-123" style="padding: 5px 10px 5px 10px;">
                <div class="container">
                    <div class="col-md-5">
                        <div class="col-sm-6" style="padding: 0px;">
                            
                            <a href="Login.jsp"><img id="idLogoPageHeader" style="max-width: 210px;" class="img-responsive" /></a>
                            
                        </div>
                    </div>
                    <div class="col-md-7" style="text-align: right;">
                        <div class="form-group" style="padding: 0px;">
                            
                            <script nonce="pQg9RVb53Sc0fUywUG1nDQ==">
                                $(document).ready(function () {
                                    if('0' === "1") {
                                        header_hotline = header_hotline_minvoice;
                                    }
                                });
                            </script>
                            <p style="color: #000000;padding-top: 22px;" id="idDivLanguage">
                                <span style="color: #FF0000; font-weight: bold; font-size: 16px;height: 25px; vertical-align: middle;">HOTLINE: <script nonce="pQg9RVb53Sc0fUywUG1nDQ==">document.write(header_hotline);</script> </span>
                                <span style="margin-right: 15px;"></span>
                                <img title="English" id="idLoginBannerLanguageLeft" style="width: 18px;height: 18px;cursor: pointer;" src="Images/en_flag.png" /> | <img id="idLoginBannerLanguageRight" style="width: 18px;height: 18px;cursor: pointer;" title="Vietnamese" src="Images/vn_flag.png" />
                            </p>
                            
                        </div>
                    </div>
                    <div style="clear: both;"></div>
                </div>
                <script nonce="pQg9RVb53Sc0fUywUG1nDQ==">
                    document.addEventListener('DOMContentLoaded', function () {
                      document.getElementById('idLoginBannerLanguageLeft').addEventListener('click', function () {
                        loginEN('1');
                      });
                    });
                </script>
                <script nonce="pQg9RVb53Sc0fUywUG1nDQ==">
                    document.addEventListener('DOMContentLoaded', function () {
                      document.getElementById('idLoginBannerLanguageRight').addEventListener('click', function () {
                        loginEN('0');
                      });
                    });
                </script>
            </div>
            <script nonce="pQg9RVb53Sc0fUywUG1nDQ==">
                $(document).ready(function () {
                    if(IsWhichCA === "15" || IsWhichCA === "17") {
                        $("#idLoginBannerLanguageRight").attr("src", "Images/Laos_flag.png");
                        //document.getElementById("idDivLanguage").style.display = "none";
                        //localStorage.setItem("VN", "0");
                        document.getElementById("idLoginBannerLanguageRight").setAttribute("title", "Laos");
                    }
                });
                var image = document.getElementById('idLogoPageHeader');
                image.src = LinkLogoPage;
                if('0' === "1") {
                    image.src = "Images/Logo_Minvoice_210.png";
                }
                function loginEN(id)
                {
                    if (id === "1")
                    {
                        localStorage.setItem("VN", "0");
                    }
                    else
                    {
                        localStorage.setItem("VN", "1");
                    }
                    location.reload();
                }
            </script>
        </div>
        <div class="container">
            
            <div>
                <div style="padding: 0px 0 15px 0; text-align: center;color: #56C2E1; font-size: 20px;font-weight: bold;"><script nonce="pQg9RVb53Sc0fUywUG1nDQ==">document.write(global_fm_login_form);</script></div>
                <form name="loginform" action="" method="post" autocomplete="off">
                    <div class="col-md-3"></div>
                    <div class="col-md-6" style="border: 1px solid #56C2E1;">
                        <div style="padding: 10px;">
<!--                            <div style="padding-bottom: 10px;color: #73879C; font-weight: 600; font-size: 20px;">
                                <span id="idTitleLogin"></span>
                            </div>-->
                            <div class="form-group" style="padding: 10px 0px 10px 0px;margin: 0;">
                                <label class="control-label"><script nonce="pQg9RVb53Sc0fUywUG1nDQ==">document.write(global_fm_Username);</script></label>
                                <input class="form-control123" style="width: 100%;" name="sUserName" maxlength="50"
                                       id="sUserName" type="text" value="">
                            </div>
                            <div class="form-group" style="padding: 10px 0px 10px 0px;margin: 0;">
                                <label class="control-label"><script nonce="pQg9RVb53Sc0fUywUG1nDQ==">document.write(global_fm_Password);</script></label>
                                <div style="display: none;">
                                    <input name="sPwd123" maxlength="50" value="" id="sPwd123" type="password">
                                </div>
                                <input class="form-control123" style="width: 100%;"
                                       name="sPwd" maxlength="50" value="" id="sPwd" type="password">
                            </div>
                            <div class="form-group" style="padding: 10px 0px 0px 0px;margin: 0;display: none;">
                                <label class="control-label"><script nonce="pQg9RVb53Sc0fUywUG1nDQ==">document.write(login_fm_captcha);</script></label>
                            </div>
                            <div class="form-group" style="padding: 0px 0px 10px 0px;margin: 0;display: none;">
                                <div class="col-sm-5" style="padding: 0px 10px 10px 0px;">
                                    <input class="form-control123" type="text" maxlength="10" name="CaptchaText" id="idCaptchaText"/>
                                </div>
                                <div class="col-sm-6" style="padding: 0px 10px 10px 0px;">
                                    <img class="img-rounded" alt="" id="idImageCap"/>
                                    <input id="idCode" name="nameCode" style="display: none;" />
                                    <a class="idLoadImg" style="cursor: pointer;" id="hrefCaptcha">
                                        <img src="Images/refresh.png" style="height: 22px; width: 24px;">
                                    </a>
                                    <script nonce="pQg9RVb53Sc0fUywUG1nDQ==">
                                        document.addEventListener('DOMContentLoaded', function () {
                                          const token = '0.11622466619366001'; 
                                          document.getElementById('hrefCaptcha').addEventListener('click', function () {
                                            LoadCaptcha();
                                          });
                                        });
                                    </script>
                                </div>
                            </div>
                            <div class="form-group" style="padding: 10px 0px 0px 0px;margin: 0;text-align: center;">
                                <input type="button" name="btnLogin" id="btnLogin" class="buttonlog" />
                                <input type="button" name="btnFoget" id="btnFoget" class="registerlog" style="font-size: 13px;color: #000000;" />
                                <input type="hidden" name="CsrfToken" id="CsrfToken" value="0.11622466619366001"/>
                            </div>
                            <script nonce="pQg9RVb53Sc0fUywUG1nDQ==">
                                document.getElementById("sUserName").value = global_fm_Username;
                                document.getElementById("sPwd").placeholder = global_fm_Password;
                                document.getElementById("idCaptchaText").value = login_fm_captcha;
                                document.getElementById("btnLogin").value = login_fm_buton_login;
                                document.getElementById("btnFoget").value = login_fm_forget;
//                                document.getElementById("idTitleLogin").innerHTML = TitleLoginPage;
                                document.getElementById("hrefCaptcha").title = login_title_captcha;
                            </script>
                            <script nonce="pQg9RVb53Sc0fUywUG1nDQ==">
                                document.addEventListener('DOMContentLoaded', function () {
                                  const token = '0.11622466619366001'; 
                                  document.getElementById('btnLogin').addEventListener('click', function () {
                                    loginPageNoCaptcha();
                                  });
                                });
                            </script>
                        </div>
                    </div>
                    <div class="col-md-3"></div>
                </form>
                <div style="clear: both;"></div>
            </div>
            
<!--                </form>-->
                <!--<div style="clear: both;"></div>-->
            <!--</div>-->
            <a href="http://metop.info" style="display: none;">metop.info close</a>
        </div>
        <script nonce="pQg9RVb53Sc0fUywUG1nDQ==" src="js/bootstrap.min.js" nonce="pQg9RVb53Sc0fUywUG1nDQ=="></script>
        <script nonce="pQg9RVb53Sc0fUywUG1nDQ==" src="js/jquery.min.js" nonce="pQg9RVb53Sc0fUywUG1nDQ=="></script>
        <script nonce="pQg9RVb53Sc0fUywUG1nDQ==">
          document.addEventListener("DOMContentLoaded", function () {
            document.getElementById("btnFoget").addEventListener("click", fogetPage);
          });
        </script>
        
        
        <div id="footer1" style="background: #f40000;border-top:none;">
            





<div class="footer1123">
    <script nonce="pQg9RVb53Sc0fUywUG1nDQ==">
        $(document).ready(function () {
            if('0' === "1") {
                footer_address = footer_address_minvoice;
                footer_office = "";
                footer_name = footer_name_minvoice;
                footer_hotline = footer_hotline_minvoice;
                footer_email = footer_email_minvoice;
            }
        });
    </script>
    
    <div class="text-center" id="idFooterName"></div>
    <div class="text-center"><script nonce="pQg9RVb53Sc0fUywUG1nDQ==">document.write(footer_address);</script></div>
    <div class="text-center"><script nonce="pQg9RVb53Sc0fUywUG1nDQ==">document.write(footer_office);</script></div>
    
    <div class="text-center">
        Hotline: <span id="footerHotline"></span> | 
        Email: <a id="footerEmail" style="color: #ffffff; cursor: pointer;"></a>
    </div>
    
<!--    <div class="text-center">
        Hotline: <script>document.write(footer_hotline);</script>
    </div>-->
    <script nonce="pQg9RVb53Sc0fUywUG1nDQ==">
        $(document).ready(function () {
            var d = new Date();
            var n = d.getFullYear();
            document.getElementById("idFooterName").innerHTML = footer_name.replace(JS_STR_CHOISE_ANOTHER_DATE_YEAR, n);
        });
    </script>
    <div class="text-center" style="display: none">
        <script nonce="pQg9RVb53Sc0fUywUG1nDQ==">document.write(global_version_web);</script>
    </div>
    <script nonce="pQg9RVb53Sc0fUywUG1nDQ==">
        document.getElementById('footerHotline').innerText = footer_hotline;
        var emailElement = document.getElementById('footerEmail');
        emailElement.innerText = footer_email;
        emailElement.href = 'mailto:' + footer_email;
    </script>
</div>

        </div>
        
    </body>
</html>

