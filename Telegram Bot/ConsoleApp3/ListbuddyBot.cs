using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConsoleApp3
{
    public class ListbuddyBot
    {
        private Dictionary<long, string> chatStates = new Dictionary<long, string>(); 
        private Dictionary<long, ToDo> taskData = new Dictionary<long, ToDo>();
        private Dictionary<long, int> editingTaskId = new Dictionary<long, int>(); 

        TelegramBotClient botClient = new TelegramBotClient("");
        CancellationToken cancellationToken = new CancellationToken();
        ReceiverOptions receiverOptions = new ReceiverOptions { AllowedUpdates = { } };
        public async Task Start()
        {
            botClient.StartReceiving(HandlerUpdateAsync, HandlerError, receiverOptions, cancellationToken);
            var botMe = await botClient.GetMeAsync();
            Console.WriteLine($"Бот {botMe.Username} почав працювати");
            Console.ReadKey();
        }
        private Task HandlerError(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Помилка у телеграм бот API:\n{apiRequestException.ErrorCode}\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
        private async Task HandlerUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update.Message.Text != null)
            {
                await HandlerMessageAsync(botClient, update.Message);

            }
        }
        private async Task HandlerMessageAsync(ITelegramBotClient botClient, Message message)
        {
            //Виведення привітального поведомлення
            if (message.Text == "/start")
            {
                chatStates[message.Chat.Id] = "MainMenu";

                await botClient.SendTextMessageAsync(message.Chat.Id, "Ми раді зустріти вас у нашому телеграм боті \"TooDoo\"! 📝\r\n\r\nTooDoo Buddy - ваш віртуальний помічник, який допоможе організувати справи та ніколи не забувати про важливі завдання. Створюйте, редагуйте та видаляйте списки завдань з легкістю. Ваші завдання завжди під контролем!" +
                    "\r\n\r\nЯ завжди радий допомогти!\r\n\r\nP.S. Не забудь поділитися мною з друзями, щоб вони також могли організувати своє життя!");
   
                await botClient.SendTextMessageAsync(message.Chat.Id, "💌 - Додати завдання\n🗑- Видалити завдання \r\n\r\n✂️ - Редагувати завдання\n📊 - Редагувати статус завдання\r\n\r\n1 - Вивести усі завдання\n2 - Вивести завдання за номером", replyMarkup: GetMainMenuKeyboard());
                return;
            }

            //Кнопки для редагування та видалення завдання
            else if (message.Text == "✂️" | message.Text == "🗑️" && chatStates.ContainsKey(message.Chat.Id) && chatStates[message.Chat.Id] == "MainMenu")
            {               
                var client = new ApiClient();
                var tasks = await client.GetTasks();
                if (!tasks.Any())
                {
                    chatStates[message.Chat.Id] = "AddTaskFirst";
                    await botClient.SendTextMessageAsync(message.Chat.Id, "‼️ Ой-ой. Спочатку додайте своє перше завдання.");
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Натисніть \"Назад\", щоб повернутися до головного меню", replyMarkup: GetBackButtonKeyboard());
                    return;
                }

                if (message.Text == "✂️")
                {
                    chatStates[message.Chat.Id] = "EditTaskSelect";
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Виберіть завдання для редагування.", replyMarkup: GetTaskListKeyboard(tasks));
                }
                else if (message.Text == "🗑️")
                {
                    chatStates[message.Chat.Id] = "DeleteTaskSelect";
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Виберіть завдання для видалення.", replyMarkup: GetTaskListKeyboard(tasks));
                }

                return;
            }

            //Скасування дії, якщо завдання не існує
            else if (message.Text == "Назад" && chatStates.ContainsKey(message.Chat.Id) && chatStates[message.Chat.Id] == "AddTaskFirst")
            {
                chatStates[message.Chat.Id] = "MainMenu";
                await botClient.SendTextMessageAsync(message.Chat.Id, "💌 - Додати завдання\n🗑- Видалити завдання \r\n\r\n✂️ - Редагувати завдання\n📊 - Редагувати статус завдання\r\n\r\n1 - Вивести усі завдання\n2 - Вивести завдання за номером", replyMarkup: GetMainMenuKeyboard());
                return;
            }

            //Оновлення назви завдання
            else if (chatStates.ContainsKey(message.Chat.Id) && chatStates[message.Chat.Id] == "EditTaskSelect")
            {
                int taskId;
                if (int.TryParse(message.Text, out taskId))
                {
                    var client = new ApiClient();
                    var task = await client.GetTaskById(taskId);
                    if (task != null)
                    {
                        editingTaskId[message.Chat.Id] = taskId;
                        chatStates[message.Chat.Id] = "EditTaskTitle";
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"Поточна назва: {task.Title}\nВведіть нову назву завдання.");
                        await botClient.SendTextMessageAsync(message.Chat.Id, "За бажанням можете залишити назву поточною або зробити порожньою, обравши відповідний пункт меню", replyMarkup: GetCurrentBlankKeyboard());
                    }
                }
                return;
            }

            //Залишити поточним та перейти до оновлення опису завдання
            else if (message.Text == "Залишити поточним" && chatStates.ContainsKey(message.Chat.Id) && chatStates[message.Chat.Id] == "EditTaskTitle")
            {
                chatStates[message.Chat.Id] = "EditTaskDescription";
                await botClient.SendTextMessageAsync(message.Chat.Id, "Введіть новий опис завдання.");
                await botClient.SendTextMessageAsync(message.Chat.Id, "Натисніть \"Назад\", щоб повернутися до головного меню", replyMarkup: GetBackButtonKeyboard());
                return;
            }

            //Оновлення опису завдання
            else if (chatStates.ContainsKey(message.Chat.Id) && chatStates[message.Chat.Id] == "EditTaskTitle")
            {
                if (editingTaskId.ContainsKey(message.Chat.Id))
                {
                    var taskId = editingTaskId[message.Chat.Id];
                    var client = new ApiClient();
                    var task = await client.GetTaskById(taskId);

                    if (task != null)
                    {
                        task.Title = message.Text;
                        await client.UpdateTask(taskId, task);
                        chatStates[message.Chat.Id] = "EditTaskDescription";
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"Поточний опис: {task.Description}\nВведіть новий опис завдання.");
                    }
                }
                return;
            }

            //Збереження оновлень
            else if (chatStates.ContainsKey(message.Chat.Id) && chatStates[message.Chat.Id] == "EditTaskDescription")
            {
                if (editingTaskId.ContainsKey(message.Chat.Id))
                {
                    var taskId = editingTaskId[message.Chat.Id];
                    var client = new ApiClient();
                    var task = await client.GetTaskById(taskId);
                    if (task != null)
                    {
                        task.Description = message.Text;

                        await client.UpdateTask(taskId, task);

                        await botClient.SendTextMessageAsync(message.Chat.Id, $"Завдання '{task.Title}' було оновлено.");
                        chatStates[message.Chat.Id] = "MainMenu";
                        await botClient.SendTextMessageAsync(message.Chat.Id, "💌 - Додати завдання\n🗑- Видалити завдання \r\n\r\n✂️ - Редагувати завдання\n📊 - Редагувати статус завдання\r\n\r\n1 - Вивести усі завдання\n2 - Вивести завдання за номером   ", replyMarkup: GetMainMenuKeyboard());
                    }
                }
                return;
            }

            //Уточнення на видалення завдання
            else if (chatStates.ContainsKey(message.Chat.Id) && chatStates[message.Chat.Id] == "DeleteTaskSelect")
            {
                int taskId;
                if (int.TryParse(message.Text, out taskId))
                {
                    chatStates[message.Chat.Id] = $"DeleteTaskConfirm:{taskId}";
                    var client = new ApiClient();
                    var task = await client.GetTaskById(taskId);
                    if (task != null)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"Ви впевнені, що хочете видалити завдання '{task.Title}'?", replyMarkup: GetConfirmationKeyboard());
                    }                   
                }
                return;
            }

            //Видалення завдання та скасування його видалення
            else if (chatStates.ContainsKey(message.Chat.Id) && chatStates[message.Chat.Id].StartsWith("DeleteTaskConfirm:"))
            {
                var state = chatStates[message.Chat.Id];
                int taskId = int.Parse(state.Split(':')[1]);

                if (message.Text == "Так")
                {
                    var client = new ApiClient();
                    var task = await client.GetTaskById(taskId);
                    if (task != null)
                    {
                        await client.DeleteTaskById(taskId);
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"Завдання '{task.Title}' було успішно видалено.");
                    }
                }
                
                else if (message.Text == "Ні")
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Видалення завдання скасовано.", replyMarkup: GetMainMenuKeyboard());
                }

                chatStates[message.Chat.Id] = "MainMenu";
                await botClient.SendTextMessageAsync(message.Chat.Id, "💌 - Додати завдання\n🗑- Видалити завдання \r\n\r\n✂️ - Редагувати завдання\n📊 - Редагувати статус завдання\r\n\r\n1 - Вивести усі завдання\n2 - Вивести завдання за номером", replyMarkup: GetMainMenuKeyboard());

                return;
            }
          
            //Додання назви нового завдання
            else if (message.Text == "💌" && chatStates.ContainsKey(message.Chat.Id) && chatStates[message.Chat.Id] == "MainMenu")
            {
                chatStates[message.Chat.Id] = "AddTaskTitle";
                await botClient.SendTextMessageAsync(message.Chat.Id, "Введіть назву свого завдання.");
                await botClient.SendTextMessageAsync(message.Chat.Id, "Натисніть \"Назад\", щоб повернутися до головного меню", replyMarkup: GetBackButtonKeyboard());
                return;
            }

            //Скасування додання нового завдання
            else if (message.Text == "Назад" && chatStates.ContainsKey(message.Chat.Id) && chatStates[message.Chat.Id] == "AddTaskTitle")
            {
                chatStates[message.Chat.Id] = "MainMenu";       
                await botClient.SendTextMessageAsync(message.Chat.Id, "💌 - Додати завдання\n🗑- Видалити завдання \r\n\r\n✂️ - Редагувати завдання\n📊 - Редагувати статус завдання\r\n\r\n1 - Вивести усі завдання\n2 - Вивести завдання за номером", replyMarkup: GetMainMenuKeyboard());
                return;
            }

            //Додання описа нового завдання
            else if (chatStates.ContainsKey(message.Chat.Id) && chatStates[message.Chat.Id] == "AddTaskTitle")
            {
                string taskTitle = message.Text;

                if (!taskData.ContainsKey(message.Chat.Id))
                {
                    taskData[message.Chat.Id] = new ToDo();
                }

                taskData[message.Chat.Id].Title = taskTitle;

                chatStates[message.Chat.Id] = "AddTaskDescription";

                await botClient.SendTextMessageAsync(message.Chat.Id, "Введіть опис свого завдання.");
                await botClient.SendTextMessageAsync(message.Chat.Id, "Натисніть \"Назад\", щоб повернутися до головного меню", replyMarkup: GetBackButtonKeyboard());
                return; 
            }

            //Скасування додання нового завдання після введення назви
            else if (message.Text == "Назад" && chatStates.ContainsKey(message.Chat.Id) && chatStates[message.Chat.Id] == "AddTaskDescription")
            {
                chatStates[message.Chat.Id] = "MainMenu";
                await botClient.SendTextMessageAsync(message.Chat.Id, "💌 - Додати завдання\n🗑- Видалити завдання \r\n\r\n✂️ - Редагувати завдання\n📊 - Редагувати статус завдання\r\n\r\n1 - Вивести усі завдання\n2 - Вивести завдання за номером", replyMarkup: GetMainMenuKeyboard());
            }

            //Збереження додання нового завдання
            else if (chatStates.ContainsKey(message.Chat.Id) && chatStates[message.Chat.Id] == "AddTaskDescription")
            {
                string taskDescription = message.Text;
                if (taskData.ContainsKey(message.Chat.Id))
                {
                    taskData[message.Chat.Id].Description = taskDescription;
                    taskData[message.Chat.Id].Status = "Очікує виконання";

                    var newTask = taskData[message.Chat.Id];
                    var client = new ApiClient();
                    var createdTask1 = await client.AddTask(taskData[message.Chat.Id]);

                    
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Завдання '{createdTask1.Title}' було додано з ідентифікатором {createdTask1.Id}");
                    await botClient.SendTextMessageAsync(message.Chat.Id, "💌 - Додати завдання\n🗑- Видалити завдання \r\n\r\n✂️ - Редагувати завдання\n📊 - Редагувати статус завдання\r\n\r\n1 - Вивести усі завдання\n2 - Вивести завдання за номером", replyMarkup: GetMainMenuKeyboard());

                    taskData.Remove(message.Chat.Id);               
                    chatStates[message.Chat.Id] = "MainMenu";
                }
            }

            //Оновлення статусу завдання
            else if (message.Text == "📊" && chatStates.ContainsKey(message.Chat.Id) && chatStates[message.Chat.Id] == "MainMenu")
            {
                var client = new ApiClient();
                var tasks = await client.GetTasks();
                chatStates[message.Chat.Id] = "UpdateTaskStatus";
                await botClient.SendTextMessageAsync(message.Chat.Id, "Виберіть завдання для оновлення його статусу.", replyMarkup: GetTaskListKeyboard(tasks));
                return;
            }

            else if (chatStates.ContainsKey(message.Chat.Id) && chatStates[message.Chat.Id] == "UpdateTaskStatus")
            {
                int taskId;
                if (int.TryParse(message.Text, out taskId))
                {

                    chatStates[message.Chat.Id] = $"UpdateTaskStatusConfirm:{taskId}";
                    var client = new ApiClient();
                    var task = await client.GetTaskById(taskId);
                    if (task != null)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"Виберіть новий статус для завдання '{task.Title}'.", replyMarkup: GetUpdateStatusKeyboard());
                    }
                }
                return;
            }

            //Збереження оновлення статусу завдання
            else if (chatStates.ContainsKey(message.Chat.Id) && chatStates[message.Chat.Id].StartsWith("UpdateTaskStatusConfirm:"))
            {
                var state = chatStates[message.Chat.Id];
                int taskId = int.Parse(state.Split(':')[1]);

                var newStatus = message.Text;
                var client = new ApiClient();
                var task = await client.GetTaskById(taskId);
                if (task != null)
                {
                    var validStatuses = new[] { "У процесі", "Виконано", "Очікує виконання", "Пропущено", "Скрите" };
                    if (validStatuses.Contains(newStatus))
                    {
                        task.Status = newStatus;
                        await client.UpdateTask(taskId, task);
                        await client.UpdateTaskStatus(taskId, task);
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"Статус завдання '{task.Title}' успішно оновлено на '{task.Status}'.");
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"Неправильний статус. Будь ласка, виберіть статус зі списку.");
                        return;
                    }
                }

                chatStates[message.Chat.Id] = "MainMenu";
                await botClient.SendTextMessageAsync(message.Chat.Id, "1 - Вивести усі завдання\n2 - Вивести завдання за номером\r\n\r\n💌 - Додати завдання\n🗑- Видалити завдання \r\n\r\n✂️ - Редагувати завдання\n📊 - Редагувати статус завдання", replyMarkup: GetMainMenuKeyboard());
                return;
            }


            // Виведення усіх завдань
            if (message.Text == "1" && chatStates.ContainsKey(message.Chat.Id) && chatStates[message.Chat.Id] == "MainMenu")
            {              
                var client = new ApiClient();
                var tasks = await client.GetTasks();
                string response = "Усі завдання:\n";
                foreach (var task in tasks)
                {
                    response += $"{task.Id}. {task.Title}\n{task.Description}\n{task.Status}\r\n\r\n";
                }
                await botClient.SendTextMessageAsync(message.Chat.Id, response);
                
                chatStates[message.Chat.Id] = "MainMenu";
                await botClient.SendTextMessageAsync(message.Chat.Id, "💌 - Додати завдання\n🗑- Видалити завдання \r\n\r\n✂️ - Редагувати завдання\n📊 - Редагувати статус завдання\r\n\r\n1 - Вивести усі завдання\n2 - Вивести завдання за номером", replyMarkup: GetMainMenuKeyboard());               
                return;
            }

            // Виведення завдання за id
            if (message.Text == "2" && chatStates.ContainsKey(message.Chat.Id) && chatStates[message.Chat.Id] == "MainMenu")
            {
                var client = new ApiClient();
                var tasks = await client.GetTasks();
                chatStates[message.Chat.Id] = "GetTaskById";
                await botClient.SendTextMessageAsync(message.Chat.Id, "Виберіть завдання для виведення", replyMarkup: GetTaskListKeyboard(tasks));
                return;
            }
            // Виведення завдання за id
            if (chatStates.ContainsKey(message.Chat.Id) && chatStates[message.Chat.Id] == "GetTaskById")
            {
                int taskId;
                if (int.TryParse(message.Text, out taskId))
                {
                    var client = new ApiClient();
                    var task = await client.GetTaskById(taskId);
                    if (task != null)
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Ваше завдання:\n{task.Id}. {task.Title}\n{task.Description}\n{task.Status}");
                }
                chatStates[message.Chat.Id] = "MainMenu";
                await botClient.SendTextMessageAsync(message.Chat.Id, "💌 - Додати завдання\n🗑- Видалити завдання \r\n\r\n✂️ - Редагувати завдання\n📊 - Редагувати статус завдання\r\n\r\n1 - Вивести усі завдання\n2 - Вивести завдання за номером", replyMarkup: GetMainMenuKeyboard());
                return;
            }
        }

        //Кнопки головного меню
        private ReplyKeyboardMarkup GetMainMenuKeyboard()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] {"💌", "🗑️", "✂️", "📊" },
                new KeyboardButton[] {"1", "2"},
            })
            {
                ResizeKeyboard = true
            };
        }

        //Кнопка скасування дії
        private ReplyKeyboardMarkup GetBackButtonKeyboard()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] {"Назад"}
            })
            {
                ResizeKeyboard = true
            };
        }

        //Кнопка виведення індифікаторів завдань
        private ReplyKeyboardMarkup GetTaskListKeyboard(List<ToDo> tasks)
        {
            var buttons = tasks.Select(task => new KeyboardButton(task.Id.ToString())).ToArray();
            return new ReplyKeyboardMarkup(buttons)
            {
                ResizeKeyboard = true
            };
        }

        //Кнопка для пропуску дії
        private ReplyKeyboardMarkup GetCurrentBlankKeyboard()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] {"Залишити поточним"}
            })
            {
                ResizeKeyboard = true
            };
        }

        //Кнопка для підтвердження дії
        private ReplyKeyboardMarkup GetConfirmationKeyboard()
        {
            return new ReplyKeyboardMarkup(new[]
            {
        new[] { new KeyboardButton("Так"), new KeyboardButton("Ні") }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true
            };
        }

        //Кнопки зміни статуса
        private ReplyKeyboardMarkup GetUpdateStatusKeyboard()
        {
            return new ReplyKeyboardMarkup(new[]
            {
            new KeyboardButton[] {"У процесі", "Виконано"},
            new KeyboardButton[] {"Очікує виконання", "Пропущено", "Скрите" }
        })
            {
                ResizeKeyboard = true
            };
        }
    }
}