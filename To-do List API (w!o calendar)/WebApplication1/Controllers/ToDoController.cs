﻿using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using ToDoApi.Model;
using WebApplication1;

namespace ToDoApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ToDoController : ControllerBase
    {
        private static List<ToDo> toDoTasks = new List<ToDo>();

        //Отримання усіх заваднь
        [HttpGet]
        public IActionResult GetAllTasks()
        {

            return Ok(toDoTasks);
        }

        //Отримання завдання за його id
        [HttpGet("{id}")]
        public IActionResult GetTaskById(int id)
        {
            var task = toDoTasks.Find(t => t.Id == id);
            if (task == null)
            {
                return NotFound();
            }
            return Ok(task);
        }

        //Створення завдання
        [HttpPost]
        public async Task<IActionResult> AddTask([FromBody] ToDo newTask)
        {
            if (newTask == null)
            {
                return BadRequest("Invalid data.");
            }

            newTask.Id = GenerateId();
            toDoTasks.Add(newTask);

            return CreatedAtAction(nameof(GetAllTasks), new { id = newTask.Id }, newTask);
        }


        //Оновлення завдання за його id
        [HttpPut("{id}")]
        public IActionResult UpdateTask(int id, [FromBody] ToDo updatedTask)
        {
            var existingTask = toDoTasks.Find(t => t.Id == id);
            if (existingTask == null)
            {
                return NotFound();
            }

            if (updatedTask == null || id != updatedTask.Id)
            {
                return BadRequest("Invalid data or mismatched IDs.");
            }

            existingTask.Title = updatedTask.Title;
            existingTask.Description = updatedTask.Description;

            return Ok(existingTask);
        }

        //Оновлення статусу завдання за його id
        [HttpPut("{id}/complete")]
        public IActionResult CompleteTask(int id, [FromBody] string status)
        {
            var task = toDoTasks.Find(t => t.Id == id);
            if (task == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(status))
            {
                return BadRequest("Invalid status.");
            }           
            if (status != "Очікує виконання" && status != "Виконано" && status != "У процесі" && status != "Пропущено" && status != "Скрите")
            {
                return BadRequest("Недопустимий статус. Статус повинен бути 'очікує виконання', 'виконано', 'у процесі', 'пропущено' або 'скрите'.");
            }

            task.Status = status;

            return Ok(task);
        }

        //Видалення завдання
        [HttpDelete("{id}")]
        public IActionResult DeleteTask(int id)
        {
            var taskToRemove = toDoTasks.Find(t => t.Id == id);
            if (taskToRemove == null)
            {
                return NotFound();
            }

            toDoTasks.Remove(taskToRemove);

            return NoContent();
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