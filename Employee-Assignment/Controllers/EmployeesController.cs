using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    [HttpGet]
    public IActionResult GetEmployees()
    {
        var employees = new[]
        {
            new { id = 1, Name = "Tazeen", Email = "tazeenedil470@gmail.com", Position= "Intern" },
            new { id = 2, Name = "Araish", Email = "araishedil1@gmail.com", Position= "Software Engineer" },
        };

        return Ok(employees);
    }

    [HttpPost]
    public IActionResult CreateEmployee([FromBody] EmployeeDto employee)
    {
        return Ok(new { message = "Employee created", employee });
    }
}

public class EmployeeDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Position { get; set; }
}
