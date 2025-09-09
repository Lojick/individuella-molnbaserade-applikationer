document.getElementById("visitorForm").addEventListener("submit", async function (e) {
    e.preventDefault(); //stoppa omladdning

    const formData = new FormData(this);

    const response = await fetch("https://individuellafunctionapp-etfvbqhme4eqavfn.germanywestcentral-01.azurewebsites.net/api/HttpRegisterVisitor", {
        method: "POST",
        body: formData
    });

    const message = document.getElementById("message");

    if (!response.ok) {
        // Ifall det blir fel = röd text
        const errorText = await response.text();
        message.innerText = errorText;
        message.style.color = "red";
        message.style.fontSize = "18px";
    }
    else {
        // Ifall det blir ok = grön text
        const result = await response.text();
        message.innerText = result;
        message.style.color = "green";
        message.style.fontSize = "18px";
        this.reset(); // töm formuläret
    }
});