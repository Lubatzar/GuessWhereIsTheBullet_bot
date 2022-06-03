using System.Diagnostics;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

Dictionary<long, User> users = new Dictionary<long, User>();

var botClient = new TelegramBotClient("5563190089:AAF_3mu0YPRnxYMzYGrk7gPUEbdl-MyYpwk");

using var cts = new CancellationTokenSource();

// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
};
botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    errorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    // Only process Message updates: https://core.telegram.org/bots/api#message
    if (update.Type != UpdateType.Message)
        return;
    // Only process text messages
    if (update.Message!.Type != MessageType.Text)
        return;
    
    Message sentMessage;
    
    var chatId = update.Message.Chat.Id;
    if (!users.ContainsKey(chatId))
    {
        users.Add(chatId, new User());
        sentMessage = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "О, новый пользователь!\n" +
                  "Добро пожаловать в бота \"Угадай где пуля\"\n" +
                  "a.k.a Русская рулетка\n" +
                  "Узнай свою удачу на сегодняшний день",
            cancellationToken: cancellationToken,
            replyMarkup: new ReplyKeyboardMarkup(new []
            {
                new KeyboardButton[] { "Испытать удачу" },
            })
            {
                ResizeKeyboard = true
            });
        return;
    }

    User user = users[chatId];
        
    var messageText = update.Message.Text;

    Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");

    // Echo received message text
    
    if (messageText == "Испытать удачу")
    {
        DateTime timeleft = new DateTime() + (DateTime.Today.AddDays((1)) - DateTime.Now);
        
        if (DateTime.Now > user.Date.AddDays(1))
        {
            // 83.3%
            // 80.0%
            // 75.0%
            // 66.6%
            // 50.0%
            var chanse = new Random();
            
            if (chanse.Next(100) < user.Chanse)
            {
                sentMessage = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Ты выжил! ;)\n" +
                          $"Вероятность, что ты бы вышел была {user.Chanse.ToString("N2")}%\n" +
                          "Неплохо, продолжим дальше",
                    cancellationToken: cancellationToken);
                user.Chanse *= (double) chanse.Next(9400, 9600) / 10000;
                user.Shots += 1;
                
                if (user.Shots == 5)
                {
                    sentMessage = await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Братик, ты пережил 5 пуль, я не дам тебе убить себя\n" +
                              "Твоя удача на сегодняшний день максимальна, сходи в лотерею, штоле\n" +
                              $"Попробуешь снова через {timeleft:hh} часов {timeleft:mm} минут ;)",
                        cancellationToken: cancellationToken);
                        user.Date += DateTime.Today.AddDays((1)) - DateTime.Now;
                        user.Chanse = 83.33;
                }
            }
            else
            {
                sentMessage = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Ты умер! ;)\n" +
                          $"Вероятность, что ты бы выжил была {user.Chanse.ToString("N2")}%\n" +
                          $"Но ты unlucky, умер на {user.Shots + 1} пуле\n" +
                          $"Ты воскреснешь через {timeleft:hh} часов {timeleft:mm} минут ;)",
                    cancellationToken: cancellationToken);
                user.Date += DateTime.Today.AddDays((1)) - DateTime.Now;
                user.Chanse = 83.33;
            }
            
            
        }
        else
        {
            sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Попыток на сегодня не осталось!\n" +
                      $"Возвращайся через {timeleft:hh} часов {timeleft:mm} минут\n",
                cancellationToken: cancellationToken);
            user.Shots = 0;
        }
    }
    else if (messageText == ".ьутг" || messageText == ".ыефке")
    {
        sentMessage = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Дибил, раскладку поменяй",
            cancellationToken: cancellationToken);
    }
}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}

class User
{
    public DateTime Date { get; set; } = DateTime.Now - TimeSpan.FromDays(1);
    public double Chanse { get; set; } = 83.33;
    public int Shots { get; set; } = 0;
}