using System.Text;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.Playwright;
using Telegram.Bot.Examples.WebHook.Services;
using System;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Telegram.Bot.Services;

public class UpdateHandlers
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<UpdateHandlers> _logger;
    private readonly BrowserHandlers _browserHandlers;
    private readonly TradingView _tradingView;
    private readonly Cnyes _cnyes;
    private int StockNumber;

    public UpdateHandlers(ITelegramBotClient botClient, ILogger<UpdateHandlers> logger, BrowserHandlers browserHandlers,
                          TradingView tradingView, Cnyes cnyes)
    {
        _botClient = botClient;
        _logger = logger;
        _browserHandlers = browserHandlers;
        _tradingView = tradingView;
        _cnyes = cnyes;
    }

    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        var handler = update switch
        {
            // UpdateType.Unknown:
            // UpdateType.ChannelPost:
            // UpdateType.EditedChannelPost:
            // UpdateType.ShippingQuery:
            // UpdateType.PreCheckoutQuery:
            // UpdateType.Poll:
            { Message: { } message } => BotOnMessageReceived(message, cancellationToken),
            { EditedMessage: { } message } => BotOnMessageReceived(message, cancellationToken),
            { CallbackQuery: { } callbackQuery } => BotOnCallbackQueryReceived(callbackQuery, cancellationToken),
            { InlineQuery: { } inlineQuery } => BotOnInlineQueryReceived(inlineQuery, cancellationToken),
            { ChosenInlineResult: { } chosenInlineResult } => BotOnChosenInlineResultReceived(chosenInlineResult, cancellationToken),
            _ => UnknownUpdateHandlerAsync(update, cancellationToken)
        };

        await handler;
    }

    private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Receive message type: {MessageType}", message.Type);
        if (message.Text is not { } messageText)
            return;

        if (messageText == "/start" || messageText == "hello")
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Hello " + message.From.FirstName + " " + message.From.LastName + "",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
        }
        else if (messageText.Split().ToList().Count >= 2)
        {
            try
            {
                if (_browserHandlers._browser == null)
                    await _browserHandlers.CreateBrowserAsync();

                var text = messageText.Split().ToList();
                int.TryParse(text[1], out StockNumber);

                _logger.LogInformation("讀取網站中...");

                #region 測試網址
                if (text[0] == "/url")
                {
                    await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "2023/10 已移除此功能，請使用其他功能",
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);

                    //if (text.Count == 2)
                    //{
                    //    Console.WriteLine($"讀取網站中...");
                    //    await _browserHandlers._page.GotoAsync($"{text[1]}",
                    //        new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle }).WaitAsync(new TimeSpan(0, 1, 0));
                    //    Console.WriteLine($"存取圖片中...");
                    //    Stream stream = new MemoryStream(await _browserHandlers._page.ScreenshotAsync());
                    //    sentMessage = await botClient.SendPhotoAsync(
                    //    chatId: chatId,
                    //    photo: stream,
                    //    parseMode: ParseMode.Html,
                    //    cancellationToken: cancellationToken);
                    //}
                }
                #endregion

                #region TradingView

                else if (text[0] == "/chart")
                {
                    if (text.Count == 2)
                    {
                        var reply = await _botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: @$"<b>-讀取中，請稍後⏰-</b>",
                            replyMarkup: new ReplyKeyboardRemove(),
                            parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken);

                        await _tradingView.GetChartAsync(StockNumber, message.Chat.Id, cancellationToken);

                        await _botClient.DeleteMessageAsync(
                            chatId: message.Chat.Id,
                            messageId: reply.MessageId,
                            cancellationToken);
                    }
                }

                else if (text[0] == "/range")
                {
                    var reply = await _botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: @$"<b>-讀取中，請稍後-⏰</b>",
                        replyMarkup: new ReplyKeyboardRemove(),
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken);

                    if (text.Count == 3)
                    {
                        await _tradingView.GetRangeAsync(StockNumber, message.Chat.Id, text[2], cancellationToken);
                    }
                    else
                    {
                        await _tradingView.GetRangeAsync(StockNumber, message.Chat.Id, null, cancellationToken);
                    }

                    await _botClient.DeleteMessageAsync(
                        chatId: message.Chat.Id,
                        messageId: reply.MessageId,
                        cancellationToken);
                }
                #endregion

                #region 鉅亨網
                //K線
                else if (text[0] == "/k")
                {
                    string range = "日K";
                    if (text.Count == 3)
                    {
                        switch (text[2].ToLower())
                        {
                            case "h":
                                range = "分時";
                                break;
                            case "d":
                                range = "日K";
                                break;
                            case "w":
                                range = "週K";
                                break;
                            case "m":
                                range = "月K";
                                break;
                            case "5m":
                                range = "5分";
                                break;
                            case "10m":
                                range = "10分";
                                break;
                            case "15m":
                                range = "15分";
                                break;
                            case "30m":
                                range = "30分";
                                break;
                            case "60m":
                                range = "60分";
                                break;
                            default:
                                await _botClient.SendTextMessageAsync(
                                    chatId: message.Chat.Id,
                                    text: "指令錯誤請重新輸入",
                                    cancellationToken: cancellationToken);
                                break;
                        }
                    }
                    var reply = await _botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: @$"<b>-讀取中，請稍後⏰-</b>",
                        replyMarkup: new ReplyKeyboardRemove(),
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken);

                    await _cnyes.GetKlineAsync(StockNumber, message.Chat.Id, range, cancellationToken);

                    await _botClient.DeleteMessageAsync(
                        chatId: message.Chat.Id,
                        messageId: reply.MessageId,
                        cancellationToken);
                }
                //詳細報價
                else if (text[0] == "/v")
                {
                    if (text.Count == 2)
                    {
                        var reply = await _botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: @$"<b>-讀取中，請稍後⏰-</b>",
                            replyMarkup: new ReplyKeyboardRemove(),
                            parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken);

                        await _cnyes.GetDetialPriceAsync(StockNumber, message.Chat.Id, cancellationToken);

                        await _botClient.DeleteMessageAsync(
                            chatId: message.Chat.Id,
                            messageId: reply.MessageId,
                            cancellationToken);
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(
                                    chatId: message.Chat.Id,
                                    text: "指令錯誤請重新輸入",
                                    cancellationToken: cancellationToken);
                    }
                }
                //績效
                else if (text[0] == "/p")
                {
                    if (text.Count == 2)
                    {
                        var reply = await _botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: @$"<b>-讀取中，請稍後⏰-</b>",
                            replyMarkup: new ReplyKeyboardRemove(),
                            parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken);

                        await _cnyes.GetPerformanceAsync(StockNumber, message.Chat.Id, cancellationToken);

                        await _botClient.DeleteMessageAsync(
                            chatId: message.Chat.Id,
                            messageId: reply.MessageId,
                            cancellationToken);
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(
                                    chatId: message.Chat.Id,
                                    text: "指令錯誤請重新輸入",
                                    cancellationToken: cancellationToken);
                    }
                }
                //新聞
                else if (text[0] == "/n")
                {
                    if (text.Count == 2)
                    {
                        var reply = await _botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: @$"<b>-讀取中，請稍後⏰-</b>",
                            replyMarkup: new ReplyKeyboardRemove(),
                            parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken);

                        await _cnyes.GetNewsAsync(StockNumber, message.Chat.Id, cancellationToken);

                        await _botClient.DeleteMessageAsync(
                            chatId: message.Chat.Id,
                            messageId: reply.MessageId,
                            cancellationToken);
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(
                                    chatId: message.Chat.Id,
                                    text: "指令錯誤請重新輸入",
                                    cancellationToken: cancellationToken);
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "錯誤");
            }
        }

        #region example
        //var action = messageText.Split(' ')[0] switch
        //{
        //    "/inline_keyboard" => SendInlineKeyboard(_botClient, message, cancellationToken),
        //    "/keyboard" => SendReplyKeyboard(_botClient, message, cancellationToken),
        //    "/remove" => RemoveKeyboard(_botClient, message, cancellationToken),
        //    "/photo" => SendFile(_botClient, message, cancellationToken),
        //    "/request" => RequestContactAndLocation(_botClient, message, cancellationToken),
        //    "/inline_mode" => StartInlineQuery(_botClient, message, cancellationToken),
        //    _ => Usage(_botClient, message, cancellationToken)
        //};
        //Message sentMessage = await action;
        //_logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);

        //Send inline keyboard
        //You can process responses in BotOnCallbackQueryReceived handler
        #endregion

    }

    static async Task<Message> SendInlineKeyboard(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        //await botClient.SendChatActionAsync(
        //    chatId: message.Chat.Id,
        //    chatAction: ChatAction.Typing,
        //    cancellationToken: cancellationToken);

        //// Simulate longer running task
        //await Task.Delay(500, cancellationToken);

        InlineKeyboardMarkup inlineKeyboard = new(
            new[]
            {
                    // first row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("時", "h"),
                        InlineKeyboardButton.WithCallbackData("日", "d"),
                        InlineKeyboardButton.WithCallbackData("週", "w"),
                        InlineKeyboardButton.WithCallbackData("月", "m"),
                    },
                    // second row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("5分", "5m"),
                        InlineKeyboardButton.WithCallbackData("10分", "10m"),
                        InlineKeyboardButton.WithCallbackData("15分", "15m"),
                        InlineKeyboardButton.WithCallbackData("30分", "30m"),
                        InlineKeyboardButton.WithCallbackData("60分", "60m"),
                    },
            });

        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "請選擇K線週期",
            replyMarkup: inlineKeyboard,
            cancellationToken: cancellationToken);
    }

    static async Task<Message> SendReplyKeyboard(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        ReplyKeyboardMarkup replyKeyboardMarkup =
            new(
            new[]
            {
                        new KeyboardButton[] { "1.1", "1.2" },
                        new KeyboardButton[] { "2.1", "2.2" },
            })
            {
                ResizeKeyboard = true,
            };
        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Choose",
            replyMarkup: replyKeyboardMarkup,
            cancellationToken: cancellationToken);
    }

    static async Task<Message> RemoveKeyboard(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Removing keyboard",
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }

    static async Task<Message> SendFile(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        await botClient.SendChatActionAsync(
            message.Chat.Id,
            ChatAction.UploadPhoto,
            cancellationToken: cancellationToken);

        const string filePath = "Files/tux.png";
        await using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var fileName = filePath.Split(Path.DirectorySeparatorChar).Last();

        return await botClient.SendPhotoAsync(
            chatId: message.Chat.Id,
            photo: new InputFileStream(fileStream, fileName),
            caption: "Nice Picture",
            cancellationToken: cancellationToken);
    }

    static async Task<Message> RequestContactAndLocation(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        ReplyKeyboardMarkup RequestReplyKeyboard = new(
            new[]
            {
                    KeyboardButton.WithRequestLocation("Location"),
                    KeyboardButton.WithRequestContact("Contact"),
            });

        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Who or Where are you?",
            replyMarkup: RequestReplyKeyboard,
            cancellationToken: cancellationToken);
    }

    static async Task<Message> Usage(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        const string usage = "Usage:\n" +
                             "/inline_keyboard - send inline keyboard\n" +
                             "/keyboard    - send custom keyboard\n" +
                             "/remove      - remove custom keyboard\n" +
                             "/photo       - send a photo\n" +
                             "/request     - request location or contact\n" +
                             "/inline_mode - send keyboard with Inline Query";

        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: usage,
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }

    static async Task<Message> StartInlineQuery(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        InlineKeyboardMarkup inlineKeyboard = new(
            InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Inline Mode"));

        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Press the button to start Inline Query",
            replyMarkup: inlineKeyboard,
            cancellationToken: cancellationToken);

    }


    // Process Inline Keyboard callback data
    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);
        var i = StockNumber;
        string range;

        switch (callbackQuery.Data)
        {
            case "1d":
                range = "1D";
                break;
            case "5d":
                range = "5D";
                break;
            case "1m":
                range = "1M";
                break;
            case "3m":
                range = "3M";
                break;
            case "6m":
                range = "6M";
                break;
            case "ytd":
                range = "YTD";
                break;
            case "1y":
                range = "12M";
                break;
            case "5y":
                range = "60M";
                break;
            case "all":
                range = "ALL";
                break;
            default:
                range = "YTD";
                break;
        }

        await _botClient.SendTextMessageAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            text: $"Received {callbackQuery.Data}",
            cancellationToken: cancellationToken);

    }


    #region Inline Mode

    private async Task BotOnInlineQueryReceived(InlineQuery inlineQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline query from: {InlineQueryFromId}", inlineQuery.From.Id);

        InlineQueryResult[] results = {
            // displayed result
            new InlineQueryResultArticle(
                id: "1",
                title: "TgBots",
                inputMessageContent: new InputTextMessageContent("hello"))
        };

        await _botClient.AnswerInlineQueryAsync(
            inlineQueryId: inlineQuery.Id,
            results: results,
            cacheTime: 0,
            isPersonal: true,
            cancellationToken: cancellationToken);
    }

    private async Task BotOnChosenInlineResultReceived(ChosenInlineResult chosenInlineResult, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline result: {ChosenInlineResultId}", chosenInlineResult.ResultId);

        await _botClient.SendTextMessageAsync(
            chatId: chosenInlineResult.From.Id,
            text: $"You chose result with Id: {chosenInlineResult.ResultId}",
            cancellationToken: cancellationToken);
    }

    #endregion

    public Task HandleErrorAsync(Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogInformation("HandleError: {ErrorMessage}", ErrorMessage);
        return Task.CompletedTask;
    }

    private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
#pragma warning restore RCS1163 // Unused parameter.
#pragma warning restore IDE0060 // Remove unused parameter
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }
}
