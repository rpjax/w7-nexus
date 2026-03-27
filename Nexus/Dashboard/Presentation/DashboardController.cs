using Aidan.Core.Linq.Extensions;
using Aidan.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Nexus.Operations.Application;
using Nexus.Operations.Application.Models;
using Nexus.Accounts.Application;
using Nexus.Accounts.Aggregates;
using Nexus.Accounts.Application.Models;
namespace Nexus.Dashboard.Presentation;

[Route("api/dashboard")]
public class DashboardController : WebController
{
}
