using IISHF.Core.Attributes;
using IISHF.Core.Interfaces;
using IISHF.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace IISHF.Core.Controllers.ApiControllers;

[Route("api/[controller]")]
public class NationalMemberAssociationController : ControllerBase
{
    private readonly INMAService _nmaService;

    public NationalMemberAssociationController(INMAService nmaService)
    {
        _nmaService = nmaService;
    }


    [HttpPost]
    [Route("national-member-assocoation/post-nma-reporting-teams/reporting-year/{reportingYear}/nma/{nmaKey}")]
    [ApiKeyAuthorize]
    public async Task<IActionResult> PostNmaReportedTeams([FromBody] NmaClub club, int reportingYear, Guid nmaKey)
    {
        var nmaClub = await _nmaService.AddClub(club, reportingYear, nmaKey);

        if (nmaClub == null || nmaClub.Key == Guid.Empty)
        {
            return BadRequest();
        }

        await _nmaService.AddClubTeams(club, nmaClub);

        return new CreatedResult(nmaClub.Key.ToString(), nmaClub);
    }

}