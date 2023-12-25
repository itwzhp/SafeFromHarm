/// based on https://github.com/jannehansen/SinglePageAndAAD/blob/master/functioncall

// MSAL configuration
var msalConfig = {
    auth: {
        clientId: '06fcfbba-ea08-4743-b583-974abd5a504a',
        authority: 'https://login.microsoftonline.com/e1368d1e-3975-4ce6-893d-fc351fd44dcd',
        redirectUri: window.location.origin,
    },
    cache: {
        cacheLocation: "localStorage",
        storeAuthStateInCookie: false
    },
    system: {
        loggerOptions: {
            loggerCallback: (level, message, containsPii) => {
                console.log(message);	
            }
        }
    }
};

const myMsalObj = new msal.PublicClientApplication(msalConfig);

function handleErrors(error) {
    console.log(error);

    $('.step').hide();
    $('#error').show();
    
    $('#error-details').text(JSON.stringify(error));
}

$(function () {
    $('.step').hide();
    $('#logged-out').show();

    $("#signIn").click(function () {
        myMsalObj.loginPopup()
            .then(response => {
                // Login success
                $('#logged-out').hide();
                $('#logged-in').show();
                $('#logged-as').text(`Zalogowano jako: ${response.account.name} (${response.account.username})`);
                console.log(response);
                myMsalObj.setActiveAccount(response.account);
            })
            .catch(handleErrors);
    });

    $('#add-user').click(function () {
        const num = $('#users-form .row:last').data('num') + 1;

        $('#users-form').append(`
            <div class="row mb-4"  data-num="${num}">
                <div class="col">
                    <label for="firstname-${num}" class="form-label text-body-secondary">Imię</label>
                    <input type="input" class="form-control" id="firstname-${num}" required>
                </div>
                <div class="col">
                    <label for="lastname-${num}" class="form-label text-body-secondary">Nazwisko</label>
                    <input type="input" class="form-control" id="lastname-${num}" required>
                </div>
                <div class="col">
                    <label for="number-${num}" class="form-label text-body-secondary">Numer ewidencyjny</label>
                    <input type="input" class="form-control" id="number-${num}" required>
                </div>
            </div>
        `);
    });

    $('#create-accounts').click(function () {
        const inputs = $('#users-form input');
        const emptyInputs = inputs.filter(function() { return this.value == ""; });

        inputs.removeClass('is-invalid');
        emptyInputs.addClass('is-invalid');

        if(emptyInputs.length !== 0)
            return;

        $('#logged-in').hide();
        $('#sending').show();

        const users = [];
        $('#users-form .row').each(function () {
            const num = $(this).data('num');
            const firstname = $(`#firstname-${num}`).val();
            const lastname = $(`#lastname-${num}`).val();
            const number = $(`#number-${num}`).val();

            users.push({
                FirstName: firstname,
                LastName: lastname,
                MembershipNumber: number
            });
        });

        const data = {
            Members: users,
            RequestorEmail: myMsalObj.getActiveAccount().username
        };

        const requestScope = {
            scopes: ["https://safefromharm.zhp.pl/user_impersonation"]
        };

        myMsalObj.acquireTokenSilent(requestScope).then(function (tokenResponse)
        {
            $.ajax({
                // this URL contains the function key, but it's not the only layer of authentication - you need to authenticate with Entra as well
                url: 'https://zhp-safefromharm.azurewebsites.net/api/CreateAccounts?code=rH1fog_LYrSvwZQkUWhGwr-hht1h0DD_deRMU8h_CiHiAzFuri3pMw==',
                type: 'POST',
                data: JSON.stringify(data),
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': 'Bearer ' + tokenResponse.accessToken
                },
                contentType: 'application/json',
                success: function (results) {
                    const table = $('#accounts tbody');

                    results.forEach(function (result) {
                        let description = '';
                        switch (result.Result) {
                            case 'Success':
                                description = 'Konto zostało utworzone';
                                break;
                            case 'MemberNotInTipi':
                                description = 'Nie znaleziono aktywnego członka w Tipi';
                                break;
                            case 'MemberHasMs365':
                                description = 'Członek ma konto w MS365. Użyj jego do zalogowania się w Moodle';
                                break;
                            case 'MemberAlreadyHasMoodle':   
                                description = 'Członek ma już konto w Moodle';
                                break;
                            default:
                                description = 'Nieznany błąd';
                                break;
                        }

                        table.append(`
                            <tr>
                                <td>${result.Member.FirstName} ${result.Member.LastName}</td>
                                <td>${description}</td>
                                <td>${result.Member.MembershipNumber} / ${result.Password || ''}</td>
                            </tr>
                        `);
                    });

                    $('.step').hide();
                    $('#success').show();
                },
                error: handleErrors
            });
        })
        .catch(handleErrors);
    });
});