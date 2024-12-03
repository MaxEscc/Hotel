using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PayPalCheckoutSdk.Core;

namespace HotelReservation.Controllers
{
    public class PaypalController : Controller
    {
       

public class PayPalConfig
{
    public static PayPalHttpClient? client;

    public static void Configure()
    {
        var environment = new SandboxEnvironment("AdhE5fPavWN24K1-JZ8VS5bMqBYFYd_5cKX4Ljq9V-WGQs_xq4QWr45IN-xrPxEQT-My84REMPAFd4qA", "EFKCLonv0cfSXwpQyi65orLBwi5HUAFBXE-K0SPHqHMpM7i1tluMdtzszWDhjbLYUDRyN1QKGcSEJjoB");
        client = new PayPalHttpClient(environment);
    }
}

//

    }
}