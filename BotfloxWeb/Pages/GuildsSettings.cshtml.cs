﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Security.Claims;
using System.Threading.Tasks;
using Botflox.Bot;
using Botflox.Bot.Data;
using BotfloxWeb.Models;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BotfloxWeb.Pages
{
    [Authorize]
    public class GuildsSettings : PageModel
    {
        private readonly BotfloxBot      _botflox;
        private readonly BotfloxDatabase _database;

        public GuildsSettings(BotfloxBot botflox, BotfloxDatabase database) {
            _botflox = botflox;
            _database = database;
        }

        public IList<GuildSettingsDetailModel> OwnedGuilds { get; set; } = new List<GuildSettingsDetailModel>();

        public async Task OnGetAsync() {
            if (!ulong.TryParse(HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out ulong userId)) {
                throw new AuthenticationException("Unexpected discord api reply item type");
            }

            RestApplication application = await _botflox.Discord.GetApplicationInfoAsync();
            OwnedGuilds = await _botflox.Discord.Guilds.ToAsyncEnumerable().WhereAwait(async guild =>
                    userId == application.Owner.Id || guild.OwnerId == userId ||
                    (await _botflox.Discord.Rest.GetGuildUserAsync(guild.Id, userId))
                    .GuildPermissions.Administrator)
                .SelectAwait(async guild => new GuildSettingsDetailModel(await _database.GuildsSettings
                    .FindAsync(guild.Id) ?? new GuildSettings {GuildId = guild.Id, CommandPrefix = "?"}, guild.Name))
                .ToListAsync();
        }

        public async Task OnPostAsync() {
            if (!ulong.TryParse(HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out ulong userId)) {
                throw new AuthenticationException("Unexpected discord api reply item type");
            }

            ulong guildId = ulong.Parse(Request.Form["GuildId"]);
            if (userId != (await _botflox.Discord.GetApplicationInfoAsync()).Owner.Id) {
                SocketGuild guild = _botflox.Discord.GetGuild(guildId);
                if (userId != guild.OwnerId) {
                    IGuildUser? guildUser = (IGuildUser?) guild.GetUser(userId) ??
                                            await _botflox.Discord.Rest.GetGuildUserAsync(guildId, userId);
                    if (guildUser == null) {
                        throw new AuthenticationException("Logged in discord user is not part of target guild!");
                    }

                    if (!guildUser.GuildPermissions.Administrator) {
                        throw new AuthenticationException(
                            "Logged in discord user lacks admin privileges in target guild!");
                    }
                }
            }

            string commandPrefix = ((string) Request.Form["CommandPrefix"]).TrimStart();
            if (string.IsNullOrWhiteSpace(commandPrefix)) {
                throw new InvalidOperationException("Cannot set empty command prefix!");
            }

            GuildSettings settings = await _database.GuildsSettings.FindAsync(guildId) ??
                                     new GuildSettings {GuildId = guildId};
            settings.CommandPrefix = commandPrefix;
            _database.Update(settings);
            await _database.SaveChangesAsync();
        }
    }
}