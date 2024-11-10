import { DependencyContainer, inject, Lifecycle } from "tsyringe";
import fs from "fs";
import { DatabaseServer } from "@spt/servers/DatabaseServer";

import { BotDifficultyHelper } from "@spt/helpers/BotDifficultyHelper";
import { BotController } from "@spt/controllers/BotController";

import { IBotConfig } from "@spt/models/spt/config/IBotConfig";
import { IPmcConfig } from "@spt/models/spt/config/IPmcConfig";

import { ILogger } from "@spt/models/spt/utils/ILogger";

import { Difficulty, IBotType } from "@spt/models/eft/common/tables/IBotType";

import { LogTextColor } from "@spt/models/spt/logging/LogTextColor";

import { ImageRouter } from "@spt/routers/ImageRouter";

import { MailSendService } from "@spt/services/MailSendService";

import type { StaticRouterModService } from "@spt/services/mod/staticRouter/StaticRouterModService";

import path from "path";
import { RouteAction } from "@spt/di/Router";
import { HttpResponseUtil } from "@spt/utils/HttpResponseUtil";

import { IUserDialogInfo } from "@spt/models/eft/profile/ISptProfile";

import { DialogueController } from "@spt/controllers/DialogueController";
import { DialogueCallbacks } from "@spt/callbacks/DialogueCallbacks";

import { MatchCallbacks } from "@spt/callbacks/MatchCallbacks";

import { RandomUtil } from "@spt/utils/RandomUtil";
import { BotGenerator } from "@spt/generators/BotGenerator";
import { IBotBase } from "@spt/models/eft/common/tables/IBotBase";
import { BotGenerationDetails } from "@spt/models/spt/bots/BotGenerationDetails";

import { ISendMessageDetails } from "@spt/models/spt/dialog/ISendMessageDetails";
import { MessageType } from "@spt/models/enums/MessageType";
import { ProfileHelper } from "@spt/helpers/ProfileHelper";
import { IGenerateBotsRequestData } from "@spt/models/eft/bot/IGenerateBotsRequestData";
import { LocaleService } from "@spt/services/LocaleService";

import { IBots } from "@spt/models/spt/bots/IBots";
import { DatabaseService } from "@spt/services/DatabaseService";
import { ConfigServer } from "@spt/servers/ConfigServer";
import { ConfigTypes } from "@spt/models/enums/ConfigTypes";

import { ITraderConfig } from "@spt/models/spt/config/ITraderConfig";

import { KnightTrader, GeneralTrader } from "./Trader";

import { IRagfairConfig } from "@spt/models/spt/config/IRagfairConfig";

import { PreSptModLoader } from "@spt/loaders/PreSptModLoader";
import { JsonUtil } from "@spt/utils/JsonUtil";

import { KnightChatBot } from "./KnightChat";
import { IGetBodyResponseData } from "@spt/models/eft/httpResponse/IGetBodyResponseData";
import { NotificationSendHelper } from "@spt/helpers/NotificationSendHelper";
import { IGetFriendListDataResponse } from "@spt/models/eft/dialog/IGetFriendListDataResponse";
import { IMatchGroupStatusResponse } from "@spt/models/eft/match/IMatchGroupStatusResponse";
import { ItemTpl } from "@spt/models/enums/ItemTpl";

class friendlyPMC {
	config = {
		sameSideHostile: false,
		badGuy: false,
		armbands: true,
		englishBear: true,
	};

	lang: { [key: string]: any } = {};

	Logger: ILogger;
	Bots: IBotConfig;
	mailSendService: MailSendService;
	notificationSendHelper: NotificationSendHelper;
	LocaleService: LocaleService;
	randomUtil: RandomUtil;
	matchCallbacks: MatchCallbacks;

	knightTrader: KnightTrader;
	generalTrader: GeneralTrader;

	profileHelper: ProfileHelper;

	private _StringFormat(str: string, ...values: string[]) {
		return str.replace(/\{(\d+)\}/g, function (match, number) {
			return typeof values[number] != "undefined" && values[number] !== null ? values[number] : match;
		});
	}

	originalgetPmcDifficultySettings: BotDifficultyHelper["getPmcDifficultySettings"];
	originalgetBotDifficulty: BotController["getBotDifficulty"];

	originalGenerateBot: BotGenerator["generateBot"];

	botsTable: IBots;

	private _questItems = [ItemTpl.KEYCARD_TERRAGROUP_LABS_ACCESS, ItemTpl.INFO_INTELLIGENCE_FOLDER, ItemTpl.INFO_MILITARY_FLASH_DRIVE];
	private _spawnQuestItems: { [key: string]: number } = {};

	preSptLoad(container: DependencyContainer) {
		this.Logger = container.resolve("WinstonLogger");
		this.mailSendService = container.resolve("MailSendService");
		this.notificationSendHelper = container.resolve("NotificationSendHelper");
		this.LocaleService = container.resolve("LocaleService");
		this.matchCallbacks = container.resolve("MatchCallbacks");

		const configServer = container.resolve<ConfigServer>("ConfigServer");
		const databaseService = container.resolve<DatabaseService>("DatabaseService");
		const preSptModLoader: PreSptModLoader = container.resolve<PreSptModLoader>("PreSptModLoader");
		const imageRouter: ImageRouter = container.resolve("ImageRouter");
		const traderConfig: ITraderConfig = configServer.getConfig<ITraderConfig>(ConfigTypes.TRADER);
		const ragfairConfig = configServer.getConfig<IRagfairConfig>(ConfigTypes.RAGFAIR);
		const jsonUtil: JsonUtil = container.resolve<JsonUtil>("JsonUtil");

		const botGenerator = container.resolve<BotGenerator>("BotGenerator");
		const botController = container.resolve<BotController>("BotController");

		const profileHelper = container.resolve<ProfileHelper>("ProfileHelper");
		this.profileHelper = profileHelper;

		const dialogueController = container.resolve<DialogueController>("DialogueController");

		const staticRouterModService = container.resolve<StaticRouterModService>("StaticRouterModService");
		const httpResponseUtil = container.resolve<HttpResponseUtil>("HttpResponseUtil");
		const randomUtil = container.resolve<RandomUtil>("RandomUtil");
		this.randomUtil = randomUtil;

		// patch getPmcDifficultySettings as that is where we actually make the bots be friendly
		this.getPmcDifficultySettings = this.getPmcDifficultySettings.bind(this);
		container.afterResolution(
			"BotDifficultyHelper",
			(_t, result: BotDifficultyHelper) => {
				if (!this.originalgetPmcDifficultySettings) {
					this.originalgetPmcDifficultySettings = result.getPmcDifficultySettings.bind(result);
				}

				result.getPmcDifficultySettings = this.getPmcDifficultySettings;
			},
			{ frequency: "Always" }
		);

		// patch getBotDifficulty as that is where we actually make the bots be friendly
		this.getBotDifficulty = this.getBotDifficulty.bind(this);
		container.afterResolution(
			"BotController",
			(_t, result: BotController) => {
				if (!this.originalgetBotDifficulty) {
					this.originalgetBotDifficulty = result.getBotDifficulty.bind(result);
				}
				result.getBotDifficulty = this.getBotDifficulty;
			},
			{ frequency: "Always" }
		);
		// patch generateBot so that the Goons have meds
		this.generateBot = this.generateBot.bind(this);
		container.afterResolution(
			"BotGenerator",
			(_t, result: BotGenerator) => {
				if (!this.originalGenerateBot) {
					this.originalGenerateBot = result["generateBot"].bind(result);

					result["generateBot"] = this.generateBot;
				}
			},
			{ frequency: "Always" }
		);

		// add a new router for handling items being given from the squad members
		const PMCBOT: IPmcConfig = configServer.getConfig(ConfigTypes.PMC);

		const PMCBOTVALUES = {
			isUsec: PMCBOT.isUsec,
			convertIntoPmcChance: {},
		};

		for (let k in PMCBOT.convertIntoPmcChance) {
			PMCBOTVALUES.convertIntoPmcChance[k] = {};
			PMCBOTVALUES.convertIntoPmcChance[k].min = 0;
			PMCBOTVALUES.convertIntoPmcChance[k].max = 0;
		}

		let groupStatus = null;

		staticRouterModService.registerStaticRouter(
			"friendlyPMC",
			[
				new RouteAction("/singleplayer/returnitems", (url: string, info: any, sessionID: string, output: string): any => {
					const member = <IUserDialogInfo>info.member;

					const details: ISendMessageDetails = {
						recipientId: sessionID,
						sender: MessageType.USER_MESSAGE,
						senderDetails: member,
						//@prettier-ignore
						messageText: randomUtil.getArrayValue(info.alive ? this.lang.returnItems : this.lang.returnItemsDeath),
					};

					// Add items to message - recreation of sendMessageToPlayer in order to insert the NPC as a user
					if (info.items?.length > 0) {
						details.items = info.items;
						details.itemsMaxStorageLifetimeSeconds = 86400;

						// Get dialog, create if doesn't exist
						const senderDialog = this.mailSendService["getDialog"](details);

						senderDialog.Users = senderDialog.Users || [];
						senderDialog.Users.push(member); // insertion is here

						// Flag dialog as containing a new message to player
						senderDialog.new++;

						// Craft message
						const message = this.mailSendService["createDialogMessage"](senderDialog._id, details);

						// Create items array
						// Generate item stash if we have rewards.
						const itemsToSendToPlayer = this.mailSendService["processItemsBeforeAddingToMail"](senderDialog.type, details);

						// If there's items to send to player, flag dialog as containing attachments
						if ((itemsToSendToPlayer.data?.length ?? 0) > 0) {
							senderDialog.attachmentsNew += 1;
						}

						// Store reward items inside message and set appropriate flags inside message
						this.mailSendService["addRewardItemsToMessage"](message, itemsToSendToPlayer, details.itemsMaxStorageLifetimeSeconds);

						// Add message to dialog
						senderDialog.messages.push(message);

						// Send message off to player so they get it in client
						const notificationMessage = this.mailSendService["notifierHelper"].createNewMessageNotification(message);
						this.mailSendService["notificationSendHelper"].sendMessage(details.recipientId, notificationMessage);
					}

					return httpResponseUtil.emptyResponse();
				}),

				new RouteAction("/singleplayer/teamescaped", (url: string, info: any, sessionID: string, output: string): any => {
					const member: IUserDialogInfo & {
						SquadInfo: {
							Mate: boolean;
							AllyBoss?: string;
							Partial?: boolean;
							Lost?: string[];
						};
					} = info.member;

					let lostMembers = "";
					let message = this.lang.friendlyEscaped;

					let isKnightBoss = false;

					if (member.SquadInfo.AllyBoss) {
						message = this.lang.allyBossEscaped;
						isKnightBoss = member.SquadInfo.AllyBoss == "bossKnight";
					} else if (member.SquadInfo.Mate) {
						message = this.lang.teamEscaped;
						if (member.SquadInfo.Partial) {
							message = this.lang.teamSomeEscaped;
							if (member.SquadInfo.Lost.length < 3) {
								lostMembers = member.SquadInfo.Lost.map((name, i) => {
									if (i > 0) {
										if (i == member.SquadInfo.Lost.length - 1) {
											return ` and ${name}`;
										} else {
											return `, ${name}`;
										}
									} else {
										return name;
									}
								}).join("");
							} else lostMembers = "the others";
						}
					}

					let notice = this._StringFormat(randomUtil.getArrayValue(message), lostMembers);

					if (isKnightBoss) {
						this.mailSendService.sendMessageToPlayer({
							recipientId: sessionID,
							sender: MessageType.NPC_TRADER,
							//@ts-ignore
							trader: "friendlypmc-knight",
							messageText: notice,
						});
					} else {
						this.mailSendService["notificationSendHelper"].sendMessageToPlayer(sessionID, member, notice, MessageType.USER_MESSAGE);
					}

					return httpResponseUtil.emptyResponse();
				}),
				new RouteAction("/client/raid/pitconfig", (url: string, info: { Config: { sameSideHostile: boolean; badGuy: boolean; englishBear: boolean; pmcArmbands: boolean; location: string } }, sessionID: string, output: string): any => {
					this.config.armbands = info.Config.pmcArmbands;

					this.config.sameSideHostile = info.Config.sameSideHostile;
					this.config.badGuy = info.Config.badGuy;
					this.config.englishBear = info.Config.englishBear;

					this.Logger.logWithColor("friendlyPMC: Setting Server Config as " + JSON.stringify(info), LogTextColor.WHITE);

					if (this.config.armbands) {
						this.Logger.logWithColor("friendlyPMC: Adding Armbands to bots...", LogTextColor.WHITE);

						const armbandColors: Record<string, string> = {
							blue: ItemTpl.ARMBAND_BLUE,
							red: ItemTpl.ARMBAND_RED,
						};

						for (const botType in this.botsTable.types) {
							const bot = this.botsTable.types[botType];
							const equipmentArmband: Record<string, number> = {};
							switch (botType) {
								case "assaultgroup":
								case "usec":
									bot.chances.equipment.ArmBand = 100;
									equipmentArmband[armbandColors.blue] = 1;
									bot.inventory.equipment.ArmBand = equipmentArmband;
									break;
								case "bear":
									bot.chances.equipment.ArmBand = 100;
									equipmentArmband[armbandColors.red] = 1;
									bot.inventory.equipment.ArmBand = equipmentArmband;
									break;
							}
						}
					}

					if (this.config.englishBear) {
						this.Logger.logWithColor("friendlyPMC: Making all Bears speak English", LogTextColor.BLUE);
						this.botsTable.types["bear"].appearance.voice = {
							Bear_1_Eng: 1,
							Bear_2_Eng: 1,
						};
					} else {
						this.Logger.logWithColor("friendlyPMC: Making all Bears speak Russian", LogTextColor.BLUE);
						this.botsTable.types["bear"].appearance.voice = {
							Bear_1: 1,
							Bear_2: 1,
							Bear_3: 1,
						};
					}

					const userProfile = this.profileHelper.getPmcProfile(sessionID);

					this._questItems.forEach(item => {
						userProfile.Inventory.items.forEach(invItem => {
							if (invItem._tpl === item) {
								this._spawnQuestItems[item]++;
							}
						});
					});

					return httpResponseUtil.emptyResponse();
				}),
				new RouteAction("/client/game/bot/followergenerate", (url: string, info: { Info: IGenerateBotsRequestData; Preset?: string; Custom?: { Body?: string; Feet?: string; Nickname?: string; English?: boolean; Voice?: string } }, sessionID: string, output: string): any => {
					const pmcProfile = profileHelper.getPmcProfile(sessionID);

					let level = pmcProfile.Info.Level;

					const custom = info.Custom;

					this.Logger.logWithColor("friendlyPMC: Follower Options - " + JSON.stringify(info.Custom), LogTextColor.WHITE);

					const conditionPromises: IBotBase[] = [];

					for (const condition of info.Info.conditions) {
						const botGenerationDetails = botController["getBotGenerationDetailsForWave"](
							condition,
							pmcProfile,
							false,
							{
								// max should be between level and level + 5
								max: level + 5,
								// min should be between level - 5 and level
								min: Math.max(1, level - 5),
							},
							botController["botConfig"].presetBatch[condition.Role],
							false
						);

						const preparedBotBase = botGenerator["getPreparedBotBase"](
							botGenerationDetails.eventRole ?? botGenerationDetails.role, // Use eventRole if provided,
							pmcProfile.Info.Side,
							botGenerationDetails.botDifficulty
						);

						const botRole = botGenerationDetails.isPmc
							? pmcProfile.Info.Side // use side to get usec.json or bear.json when bot will be PMC
							: botGenerationDetails.role;
						const botJsonTemplateClone = botController["cloner"].clone(botController["botHelper"].getBotTemplate(botRole));

						botGenerationDetails.botRelativeLevelDeltaMax = 5;
						botGenerationDetails.botRelativeLevelDeltaMin = 5;

						// FIKA is not always spawning ARM bands for followers
						if (this.config.armbands) {
							botJsonTemplateClone.chances.equipment.ArmBand = 100;

							if (pmcProfile.Info.Side.toLowerCase() == "bear") {
								botJsonTemplateClone.inventory.equipment.ArmBand[ItemTpl.ARMBAND_RED] = 1;
							} else {
								botJsonTemplateClone.inventory.equipment.ArmBand[ItemTpl.ARMBAND_BLUE] = 1;
							}
						}

						const bot = botGenerator["generateBot"](sessionID, preparedBotBase, botJsonTemplateClone, botGenerationDetails);

						conditionPromises.push(bot);

						conditionPromises.forEach(profile => {
							if (custom) {
								if (custom.Body) {
									profile.Customization.Body = custom.Body;
								}
								if (custom.Feet) {
									profile.Customization.Feet = custom.Feet;
								}

								if (custom.Nickname) {
									profile.Info.Nickname = custom.Nickname;
									profile.Info.LowerNickname = custom.Nickname.toLowerCase();
								}
							}
							const customization = databaseService.getCustomization();

							if (custom.Voice && customization[custom.Voice]) {
								profile.Info.Voice = customization[custom.Voice]._name;
							} else if (pmcProfile.Info.Side.toLowerCase() == "bear") profile.Info.Voice = custom?.English ? `Bear_${randomUtil.getInt(1, 2)}_Eng` : `Bear_${randomUtil.getInt(1, 3)}`;
						});
					}

					const res = httpResponseUtil.getBody(conditionPromises);

					return res;
				}),

				new RouteAction("/client/game/bot/preventpmcgenerate", (url: string, info: { State: boolean }, sessionID: string, output: string): any => {
					if (info.State) {
						PMCBOT.isUsec = 0;
						for (let k in PMCBOT.convertIntoPmcChance) {
							PMCBOT.convertIntoPmcChance[k].min = 0;
							PMCBOT.convertIntoPmcChance[k].max = 0;
						}
					} else {
						PMCBOT.isUsec = PMCBOTVALUES.isUsec;
						for (let k in PMCBOT.convertIntoPmcChance) {
							PMCBOT.convertIntoPmcChance[k].min = PMCBOTVALUES.convertIntoPmcChance[k].min;
							PMCBOT.convertIntoPmcChance[k].max = PMCBOTVALUES.convertIntoPmcChance[k].max;
						}
					}

					return httpResponseUtil.emptyResponse();
				}),
				new RouteAction("/singleplayer/pitlang", (url: string, info: any, sessionID: string, output: string): any => {
					return httpResponseUtil.noBody(this.lang);
				}),
				new RouteAction("/client/match/group/invite/send", async (url: string, info: any, sessionID: string, output: string): Promise<IGetBodyResponseData<string>> => {
					const aid = info.to;

					// Knight should accept the invite
					const knightFriend = container.resolve<KnightChatBot>("KnightChatBot");

					if (aid == knightFriend.getChatBot().aid) {
						setTimeout(() => {
							knightFriend.acceptInvite(sessionID);
						}, 2000);
					}
					return this.matchCallbacks.sendGroupInvite(url, info, sessionID);
				}),
				new RouteAction("/client/friend/list", async (url: string, info: any, sessionID: string, output: string): Promise<IGetBodyResponseData<IGetFriendListDataResponse>> => {
					const list = dialogueController.getFriendList(sessionID);
					const knightFriend = container.resolve<KnightChatBot>("KnightChatBot");
					// Fika is removing Knight from the friend list, so we need to add him back
					let friend = knightFriend.getChatBot();
					if (list.Friends.findIndex(f => f.aid == friend.aid) == -1) {
						list.Friends.push(friend);
					}

					const profile = profileHelper.getPmcProfile(sessionID);

					// check if player has completed the first quest from Knight to decide if he will appear in the friend list
					let hasKnightQuest = false;
					profile.Quests.forEach(quest => {
						if (quest.qid == "friendlypmc-knight-competition" && quest.status == 4) {
							hasKnightQuest = true;
						}
					});
					if (!hasKnightQuest) {
						list.Friends = list.Friends.filter(friend => friend._id != "bossKnight");
					}
					return httpResponseUtil.getBody(list);
				}),
				new RouteAction("/client/match/raid/ready", async (url: string, info: any, sessionID: string, output: string): Promise<IGetBodyResponseData<boolean>> => {
					return httpResponseUtil.getBody(true);
				}),
				new RouteAction("/client/match/group/pitstatus", async (url: string, info: { Players: string[] }, sessionID: string, output: string) => {
					clearTimeout(groupStatus);
					groupStatus = setTimeout(() => {
						const knightFriend = container.resolve<KnightChatBot>("KnightChatBot");
						knightFriend.currentGroup = info.Players;
					});

					return httpResponseUtil.emptyResponse();
				}),
			],
			"custom-static-friendly-pmc"
		);

		const folder = path.basename(path.dirname(__dirname));
		this.knightTrader = new KnightTrader(folder, preSptModLoader, imageRouter, traderConfig, ragfairConfig, jsonUtil);
		this.generalTrader = new GeneralTrader(folder, preSptModLoader, imageRouter, traderConfig, ragfairConfig, jsonUtil);
	}

	postDBLoad(container: DependencyContainer) {
		const configServer = container.resolve<ConfigServer>("ConfigServer");

		const Bots = configServer.getConfig<IBotConfig>(ConfigTypes.BOT);
		const PMCBOT = configServer.getConfig<IPmcConfig>(ConfigTypes.PMC);

		const databaseServer = container.resolve<DatabaseServer>("DatabaseServer");

		const tables = databaseServer.getTables();

		try {
			if (fs.existsSync(`${__dirname}/../lang/en.json`)) {
				const lg = require(`../lang/en.json`);
				this.lang = lg;
			}
		} catch (e) {
			console.error(e);
		}

		const lang = this.LocaleService.getDesiredGameLocale();
		try {
			if (lang && fs.existsSync(`${__dirname}/../lang/${lang}.json`)) {
				const lg = require(`../lang/${lang}.json`);
				this.lang = Object.assign(this.lang, lg);
			}
		} catch (e) {
			this.Logger.error("friendlyPMC: bad language file for " + lang + " - falling back to en");
			console.error(e);
		}

		// same side hostile is being changed elsewhere - do this to avoid unwanted outcome
		PMCBOT.chanceSameSideIsHostilePercent = -1;

		this.Bots = Bots;

		this.botsTable = tables.bots;

		this.knightTrader.AddToDb(tables);
		this.generalTrader.AddToDb(tables);

		container.register<KnightChatBot>("KnightChatBot", KnightChatBot, {
			lifecycle: Lifecycle.Singleton,
		});

		const knightBot = container.resolve<KnightChatBot>("KnightChatBot");
		knightBot.SetLang(this.lang);

		container.resolve<DialogueController>("DialogueController").registerChatBot(knightBot);
	}

	private _makeFriendlyOrHostile(diff: Difficulty, pmcType: string) {
		const clearWrongEnemy = (mind: Record<string, string | number | boolean | string[]>, type: string) => {
			const enemyList = <string[]>mind.ENEMY_BOT_TYPES;

			const idx = enemyList.indexOf(type);
			if (idx > -1) enemyList.splice(idx, 1);

			const idxl = enemyList.indexOf(type.toLowerCase());
			if (idxl > -1) enemyList.splice(idxl, 1);
		};

		let is_hostile = this.config.sameSideHostile || false;
		const is_bad_guy = this.config.badGuy || false;
		pmcType = pmcType.toLowerCase();

		// force the friendly mind here as some mods may overwrite things
		if (pmcType == "bear" || pmcType == "usec" || pmcType == "sptbear" || pmcType == "sptusec" || pmcType == "pmcbear" || pmcType == "pmcusec") {
			Object.assign(diff.Mind, {
				DEFAULT_ENEMY_BEAR: pmcType == "usec" || pmcType == "sptusec" || pmcType == "pmcusec" || is_hostile,
				DEFAULT_ENEMY_SAVAGE: true,
				DEFAULT_ENEMY_USEC: pmcType == "bear" || pmcType == "sptbear" || pmcType == "pmcbear" || is_hostile,
				DEFAULT_BEAR_BEHAVIOUR: !is_bad_guy && !is_hostile && (pmcType == "bear" || pmcType == "sptbear" || pmcType == "pmcbear") ? "Ignore" : "Attack",
				DEFAULT_SAVAGE_BEHAVIOUR: "Attack",
				DEFAULT_USEC_BEHAVIOUR: !is_bad_guy && !is_hostile && (pmcType == "usec" || pmcType == "sptusec" || pmcType == "pmcusec") ? "Ignore" : "Attack",
				CAN_RECIVE_PLAYER_REQUESTS: !is_hostile && !is_bad_guy,
				CAN_RECEIVE_PLAYER_REQUESTS: !is_hostile && !is_bad_guy,
				CAN_RECEIVE_PLAYER_REQUESTS_USEC: !is_hostile && !is_bad_guy,
				CAN_RECEIVE_PLAYER_REQUESTS_BEAR: !is_hostile && !is_bad_guy,
			});

			const Core: { [key: string]: any } = {};
			Core.MAX_COME_WITH_ME_REQUESTS_PER_PLAYER = 9999;
			Core.MAX_BASE_REQUESTS_PER_PLAYER = 9999;
			Core.MAX_HOLD_REQUESTS_PER_PLAYER = 9999;
			Core.MAX_GO_TO_REQUESTS_PER_PLAYER = 9999;
			Core.MAX_GET_IN_COVER_REQUESTS_PER_PLAYER = 9999;
			Core.MAX_WAIT_REQUESTS_PER_PLAYER = 9999;
			Core.START_ACTIVE_FOLLOW_PLAYER_EVENT = true;
			Core.GESTUS_MAX_ANSWERS = 9999;
			//Core.GESTUS_REQUEST_LIFETIME = 50;
			Core.GESTUS_ANYWAY_CHANCE = 0;
			Core.START_DIST_TO_COV = 20000.0;
			Core.MAX_DIST_TO_COV = 20000.0;
			Core.MAX_REQUESTS__PER_GROUP = 9999;
			Core.MAX_REQUESTS_PER_GROUP = 9999;

			Object.assign(diff.Core, Core);
			Object.assign(diff.Mind, Core, {
				FRIEND_AGR_KILL: 0.000001,
				FRIEND_DEAD_AGR_LOW: -0.000001,
			});

			if (!is_hostile) {
				if (pmcType == "bear" || pmcType == "sptbear" || pmcType == "pmcbear") {
					clearWrongEnemy(diff.Mind, "sptBear");
					clearWrongEnemy(diff.Mind, "bear");
					clearWrongEnemy(diff.Mind, "pmcBEAR");
				} else if (pmcType == "usec" || pmcType == "sptusec" || pmcType == "pmcusec") {
					clearWrongEnemy(diff.Mind, "sptUsec");
					clearWrongEnemy(diff.Mind, "usec");
					clearWrongEnemy(diff.Mind, "pmcUSEC");
				}
			}
			// ensure these settings are set last as they are not dependent of "is_hostile" flag
			Object.assign(diff.Mind, {
				ENEMY_BY_GROUPS_PMC_PLAYERS: is_hostile || is_bad_guy,
				CAN_RECEIVE_PLAYER_REQUESTS_SAVAGE: false,
			});
		} else if (pmcType == "assault") {
			Object.assign(diff.Mind, {
				FRIEND_AGR_KILL: 0.000001,
				FRIEND_DEAD_AGR_LOW: -0.000001,
			});
		}
		if (pmcType == "gifter") {
			Object.assign(diff.Mind, {
				ENEMY_BY_GROUPS_PMC_PLAYERS: false,
				REVENGE_BOT_TYPES: [],
				DEFAULT_USEC_BEHAVIOUR: "Ignore",
				DEFAULT_BEAR_BEHAVIOUR: "Ignore",
				DEFAULT_ENEMY_BEAR: false,
				DEFAULT_ENEMY_USEC: false,
			});
		}

		return diff;
	}

	/** Overwrite get difficulity method to patch the friendly/hostile settings */
	getPmcDifficultySettings(pmcType: "bear" | "usec", difficulty: string, usecType: string, bearType: string): any {
		const result = this.originalgetPmcDifficultySettings(pmcType, difficulty, usecType, bearType);

		return this._makeFriendlyOrHostile(result, pmcType);
	}
	/** Overwrite get difficulity method to patch the friendly/hostile settings */
	getBotDifficulty(type: string, difficulty: string): any {
		let result = this.originalgetBotDifficulty(type, difficulty);

		return this._makeFriendlyOrHostile(result, type);
	}

	generateBot(sessionId: string, bot: IBotBase, botJsonTemplate: IBotType, botGenerationDetails: BotGenerationDetails) {
		const role = botGenerationDetails.role.toLowerCase();
		// ensure goons have high chance of meds
		if (role == "followerbirdeye" || role == "followerbigpipe" || role == "bossknight") {
			botJsonTemplate.generation.items.healing.weights = {
				"0": 0,
				"1": 2,
				"2": 6,
			};
		}

		const userProfile = this.profileHelper.getFullProfile(sessionId);
		userProfile.characters.pmc.Quests.forEach(quest => {
			// Add quest items to Scavs if we are doing the thieves quest
			if (role == "assault" && ["tarkovstreets", "bigmap", "interchange"].includes(userProfile.inraid.location.toLowerCase())) {
				if (quest.qid == "friendlypmc-knight-thieves" && quest.status == 2) {
					if (this._spawnQuestItems[ItemTpl.KEYCARD_TERRAGROUP_LABS_ACCESS] < 3) {
						botJsonTemplate.inventory.items.Pockets[ItemTpl.KEYCARD_TERRAGROUP_LABS_ACCESS] = 500;
						botJsonTemplate.inventory.items.TacticalVest[ItemTpl.KEYCARD_TERRAGROUP_LABS_ACCESS] = 500;
						this._spawnQuestItems[ItemTpl.KEYCARD_TERRAGROUP_LABS_ACCESS]++;
					} else if (this._spawnQuestItems[ItemTpl.INFO_INTELLIGENCE_FOLDER] < 2) {
						botJsonTemplate.inventory.items.Backpack[ItemTpl.INFO_INTELLIGENCE_FOLDER] = 500;
						this._spawnQuestItems[ItemTpl.INFO_INTELLIGENCE_FOLDER]++;
					} else if (this._spawnQuestItems[ItemTpl.INFO_MILITARY_FLASH_DRIVE] < 2) {
						botJsonTemplate.inventory.items.Backpack[ItemTpl.INFO_MILITARY_FLASH_DRIVE] = 500;
						this._spawnQuestItems[ItemTpl.INFO_MILITARY_FLASH_DRIVE]++;
					}
				}
			}
		});

		const result = this.originalGenerateBot(sessionId, bot, botJsonTemplate, botGenerationDetails);

		return result;
	}
}

export const mod = new friendlyPMC();
