using Microsoft.AspNetCore.Mvc;
using ToDoApi.Model;

namespace ToDoApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ToDoController : ControllerBase
    {
        private static List<ToDo> toDoTasks = new List<ToDo>();

        [HttpGet]
        public IActionResult GetAllTasks()
        {
            return Ok(toDoTasks);
        }

        [HttpPost]
        public IActionResult AddTask([FromBody] ToDo newTask)
        {
            if (newTask == null)
            {
                return BadRequest("Invalid data.");
            }

            newTask.Id = GenerateId();
            toDoTasks.Add(newTask);
            return CreatedAtAction(nameof(GetAllTasks), new { id = newTask.Id }, newTask);
        }
        private int GenerateId()
        {
            int maxId = 0;
            foreach (var item in toDoTasks)
            {
                if (item.Id > maxId)
                {
                    maxId = item.Id;
                }
            }
            return maxId + 1;
        }
    }
}