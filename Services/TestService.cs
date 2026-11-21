using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("test")]
public class TestService
{
    [HttpPost]
    [Route("test-save")]
    public async Task TestSave(Database db)
    {
        var user = new User("test", "test");
        var reference = new Reference(Guid.CreateVersion7(), user.id.ToString(), "article", "key");

        reference.authors = ["Testi", "Testi2"];
        reference.address = "Testiosoite";

        await db.SaveUser(user);
        await db.SaveReference(reference);
    }

    [HttpGet]
    [Route("get-references")]
    public async Task<List<Reference>> GetReferences(Database db, [FromQuery] String user)
    {
        return await db.GetReferences(user);
    }
}
