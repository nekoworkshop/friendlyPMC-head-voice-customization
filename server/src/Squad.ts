import { DependencyContainer, inject, Lifecycle } from "tsyringe";
import fs from "fs";
import { DatabaseServer } from "@spt/servers/DatabaseServer";

import { BotDifficultyHelper } from "@spt/helpers/BotDifficultyHelper";
import { BotController } from "@spt/controllers/BotController";

import { IBotConfig } from "@spt/models/spt/config/IBotConfig";
import { IPmcConfig } from "@spt/models/spt/config/IPmcConfig";

import { ILogger } from "@spt/models/spt/utils/ILogger";

import { IBotType, IDifficultyCategories } from "@spt/models/eft/common/tables/IBotType";

import { LogTextColor } from "@spt/models/spt/logging/LogTextColor";

import { ImageRouter } from "@spt/routers/ImageRouter";

import { MailSendService } from "@spt/services/MailSendService";

import type { StaticRouterModService } from "@spt/services/mod/staticRouter/StaticRouterModService";

import path from "path";
import { RouteAction } from "@spt/di/Router";
import { HttpResponseUtil } from "@spt/utils/HttpResponseUtil";

import { IUserDialogInfo } from "@spt/models/eft/profile/ISptProfile";

import { DialogueController } from "@spt/controllers/DialogueController";

import { MatchCallbacks } from "@spt/callbacks/MatchCallbacks";

import { RandomUtil } from "@spt/utils/RandomUtil";
import { BotGenerator } from "@spt/generators/BotGenerator";
import { IBotBase } from "@spt/models/eft/common/tables/IBotBase";
import { IBotGenerationDetails } from "@spt/models/spt/bots/BotGenerationDetails";

import { ISendMessageDetails } from "@spt/models/spt/dialog/ISendMessageDetails";
import { MessageType } from "@spt/models/enums/MessageType";

import { ProfileHelper } from "@spt/helpers/ProfileHelper";
import { ProfileController } from "@spt/controllers/ProfileController";

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
import { ItemTpl } from "@spt/models/enums/ItemTpl";

import { ImporterUtil } from "@spt/utils/ImporterUtil";

import { PitQuestItemEventRouter } from "./Quests";
import { QuestItemEventRouter } from "@spt/routers/item_events/QuestItemEventRouter";
import { IPmcData } from "@spt/models/eft/common/IPmcData";

import { BigPipeChatBot } from "./BigPipeChat";
import { BirdEyeChatBot } from "./BirdEyeChat";

import { IGetOtherProfileRequest } from "@spt/models/eft/profile/IGetOtherProfileRequest";
import { LauncherController } from "@spt/controllers/LauncherController";

import { objectCopy } from "./Utils";
import { IGetRaidConfigurationRequestData } from "@spt/models/eft/match/IGetRaidConfigurationRequestData";
import { IAdditionalHostilitySettings } from "@spt/models/eft/common/ILocationBase";
import { ILoginRequestData } from "@spt/models/eft/launcher/ILoginRequestData";

import { Quests } from "./Quests";

class friendlyPMC {
	config = {
		friendlyPMC: false,
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
	preSptModLoader: PreSptModLoader;

	knightTrader: KnightTrader;
	generalTrader: GeneralTrader;

	knightBot: KnightChatBot;
	bigPipeBot: BigPipeChatBot;
	birdEyeBot: BirdEyeChatBot;

	profileHelper: ProfileHelper;

	databaseService: DatabaseService;
	databaseServer: DatabaseServer;

	questItemEvent: PitQuestItemEventRouter;

	private _StringFormat(str: string, ...values: string[]) {
		return str.replace(/\{(\d+)\}/g, function (match, number) {
			return typeof values[number] != "undefined" && values[number] !== null ? values[number] : match;
		});
	}

	botsTable: IBots;

	mydb: { [key: string]: any };
	myDBFolder = "";

	modFolderName: string;

	private _hostilitySettings: { [key: string]: IAdditionalHostilitySettings[] } = {};

	preSptLoad(container: DependencyContainer) {
		this.Logger = container.resolve("WinstonLogger");
		this.mailSendService = container.resolve("MailSendService");
		this.notificationSendHelper = container.resolve("NotificationSendHelper");
		this.LocaleService = container.resolve("LocaleService");
		this.matchCallbacks = container.resolve("MatchCallbacks");

		const configServer = container.resolve<ConfigServer>("ConfigServer");

		const databaseService = container.resolve<DatabaseService>("DatabaseService");
		this.databaseService = databaseService;

		const preSptModLoader: PreSptModLoader = container.resolve<PreSptModLoader>("PreSptModLoader");
		this.preSptModLoader = preSptModLoader;

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
					this.originalgetPmcDifficultySettings = result["getDifficultySettings"].bind(result);
				}

				result["getDifficultySettings"] = this.getPmcDifficultySettings;
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
		// patch getOtherProfile so that we can show our followers when "viewing other profiles"
		this.getOtherProfile = this.getOtherProfile.bind(this);
		container.afterResolution(
			"ProfileController",
			(_t, result: ProfileController) => {
				if (!this.originalGetOtherProfile) {
					this.originalGetOtherProfile = result.getOtherProfile.bind(result);
				}

				result.getOtherProfile = this.getOtherProfile;
			},
			{ frequency: "Always" }
		);
		// patch quest handder to handle our custom quests
		this.handleItemEvent = this.handleItemEvent.bind(this);
		container.afterResolution(
			"QuestItemEventRouter",
			(_t, result: QuestItemEventRouter) => {
				if (!this.originalHandleItemEvent) {
					this.originalHandleItemEvent = result.handleItemEvent.bind(result);

					result.handleItemEvent = this.handleItemEvent;
				}
			},
			{ frequency: "Always" }
		);
		// patch login to handle our custom quests item locations
		this.launcherLogin = this.launcherLogin.bind(this);
		container.afterResolution(
			"LauncherController",
			(_t, result: LauncherController) => {
				if (!this.originalLauncherLogin) {
					this.originalLauncherLogin = result.login.bind(result);

					result.login = this.launcherLogin;
				}
			},
			{ frequency: "Always" }
		);

		const PMCBOT: IPmcConfig = configServer.getConfig(ConfigTypes.PMC);

		const PMCBOTVALUES = {
			isUsec: PMCBOT.isUsec,
			convertIntoPmcChance: {},
		};

		for (let k in PMCBOT.convertIntoPmcChance) {
			PMCBOTVALUES.convertIntoPmcChance[k] = {};
			for (let j in PMCBOT.convertIntoPmcChance[k]) {
				PMCBOTVALUES.convertIntoPmcChance[k][j] = {
					min: PMCBOT.convertIntoPmcChance[k][j].min,
					max: PMCBOT.convertIntoPmcChance[k][j].max,
				};
			}
		}
		let groupStatus: { [key: string]: any } = {};

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

					if (member.SquadInfo.Mate) {
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

					this.mailSendService["notificationSendHelper"].sendMessageToPlayer(sessionID, member, notice, MessageType.USER_MESSAGE);

					return httpResponseUtil.emptyResponse();
				}),
				new RouteAction("/client/raid/pitconfig", (url: string, info: { Config: { friendlyPMC: boolean; badGuy: boolean; englishBear: boolean; pmcArmbands: boolean; location: string } }, sessionID: string, output: string): any => {
					this.config.armbands = info.Config.pmcArmbands;

					this.config.friendlyPMC = info.Config.friendlyPMC;
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
					// change bot location hostility settings based on our config
					const locations = this.databaseServer.getTables().locations;
					for (let k in locations) {
						const loc: (typeof locations)["bigmap"] = locations[k];
						if (!loc.base || loc.base.Name == "Private Sector" || loc.base.Name == "Terminal" || loc.base.Name == "Town" || loc.base.Name == "Suburbs" || loc.base.Name == "Arena") continue;

						if (!loc.base.BotLocationModifier?.AdditionalHostilitySettings) continue;

						loc.base.BotLocationModifier.AdditionalHostilitySettings = objectCopy(this._hostilitySettings[k]);

						if (!this.config.friendlyPMC) {
							if (this.config.badGuy) {
								loc.base.BotLocationModifier.AdditionalHostilitySettings.forEach(setting => {
									if (["pmcUSEC", "pmcBEAR"].includes(setting.BotRole)) {
										setting.BearPlayerBehaviour = "AlwaysEnemies";
										setting.UsecPlayerBehaviour = "AlwaysEnemies";
									}
								});
							}
						} else {
							loc.base.BotLocationModifier.AdditionalHostilitySettings.forEach(setting => {
								if (["pmcUSEC", "pmcBEAR"].includes(setting.BotRole)) {
									setting.BearPlayerBehaviour = this.config.badGuy || setting.BotRole == "pmcUSEC" ? "AlwaysEnemies" : "Neutral";
									setting.UsecPlayerBehaviour = this.config.badGuy || setting.BotRole == "pmcBEAR" ? "AlwaysEnemies" : "Neutral";
									setting.BearEnemyChance = setting.BotRole == "pmcBEAR" && this.config.friendlyPMC ? 0 : setting.BearEnemyChance;
									setting.UsecEnemyChance = setting.BotRole == "pmcUSEC" && this.config.friendlyPMC ? 0 : setting.UsecEnemyChance;
								}
							});
						}
					}

					return httpResponseUtil.emptyResponse();
				}),
				new RouteAction("/client/game/bot/followergenerate", (url: string, info: { Info: IGenerateBotsRequestData; Preset?: string; Custom?: { Body?: string; Feet?: string; Nickname?: string; English?: boolean; Voice?: string } }, sessionID: string, output: string): any => {
					const pmcProfile = profileHelper.getPmcProfile(sessionID);

					const custom = info.Custom;

					this.Logger.logWithColor("friendlyPMC: Follower Options - " + JSON.stringify(info.Custom), LogTextColor.WHITE);

					const conditionPromises: IBotBase[] = [];

					for (const condition of info.Info.conditions) {
						const raidSettings = botController["getMostRecentRaidSettings"]();
						const botGenerationDetails = botController["getBotGenerationDetailsForWave"](condition, pmcProfile, false, raidSettings, botController["botConfig"].presetBatch[condition.Role], botController["botHelper"].isBotPmc(condition.Role));

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
							for (let j in PMCBOT.convertIntoPmcChance[k]) {
								PMCBOT.convertIntoPmcChance[k][j].min = 0;
								PMCBOT.convertIntoPmcChance[k][j].max = 0;
							}
						}
					} else {
						PMCBOT.isUsec = PMCBOTVALUES.isUsec;
						for (let k in PMCBOT.convertIntoPmcChance) {
							for (let j in PMCBOT.convertIntoPmcChance[k]) {
								PMCBOT.convertIntoPmcChance[k][j].min = PMCBOTVALUES.convertIntoPmcChance[k][j].min;
								PMCBOT.convertIntoPmcChance[k][j].max = PMCBOTVALUES.convertIntoPmcChance[k][j].max;
							}
						}
					}

					return httpResponseUtil.emptyResponse();
				}),

				new RouteAction("/singleplayer/pitlang", (url: string, info: any, sessionID: string, output: string): any => {
					return httpResponseUtil.noBody(this.lang);
				}),
				new RouteAction("/singleplayer/pitprogress", (url: string, info: any, sessionID: string, output: string): any => {
					if (!this.mydb.progress || !this.mydb.progress[sessionID]) {
						return httpResponseUtil.emptyResponse();
					}
					return httpResponseUtil.noBody(this.mydb.progress[sessionID]);
				}),
				new RouteAction("/client/match/group/invite/send", async (url: string, info: any, sessionID: string, output: string): Promise<IGetBodyResponseData<string>> => {
					const aid = info.to;

					// Knight or BigPipe should accept the invite
					const knightFriend = this.knightBot;
					const pipeFriend = this.bigPipeBot;
					const birdEyeFriend = this.birdEyeBot;

					if (aid == knightFriend.getChatBot().aid) {
						setTimeout(() => {
							knightFriend.acceptInvite(sessionID);
						}, 2000);
					} else if (aid == pipeFriend.getChatBot().aid) {
						setTimeout(() => {
							pipeFriend.acceptInvite(sessionID);
						}, 2000);
					} else if (aid == birdEyeFriend.getChatBot().aid) {
						setTimeout(() => {
							birdEyeFriend.acceptInvite(sessionID);
						}, 2000);
					}
					return this.matchCallbacks.sendGroupInvite(url, info, sessionID);
				}),
				new RouteAction("/client/friend/list", async (url: string, info: any, sessionID: string, output: string): Promise<IGetBodyResponseData<IGetFriendListDataResponse>> => {
					const list = dialogueController.getFriendList(sessionID);
					try {
						const knightFriend = this.knightBot;
						const bigPipeFriend = this.bigPipeBot;
						const birdEyeFriend = this.birdEyeBot;
						// Fika is removing Knight from the friend list, so we need to add him back
						let friend = knightFriend.getChatBot();
						if (list.Friends.findIndex(f => f.aid == friend.aid) == -1) {
							list.Friends.push(friend);
						}
						// Fika is removing BigPipe from the friend list, so we need to add him back
						friend = bigPipeFriend.getChatBot();
						if (list.Friends.findIndex(f => f.aid == friend.aid) == -1) {
							list.Friends.push(friend);
						}
						// Fika is removing BirdEye from the friend list, so we need to add him back
						friend = birdEyeFriend.getChatBot();
						if (list.Friends.findIndex(f => f.aid == friend.aid) == -1) {
							list.Friends.push(friend);
						}

						const profile = profileHelper.getPmcProfile(sessionID);

						// check what quests the player has completed to know if we allow the Goons to be in his friend list
						let hasKnightQuest = false;
						let hasBigPipeQuest = false;
						let hasBirdEyeQuest = false;
						profile.Quests.forEach(quest => {
							if (quest.qid == "6775d9957e2dbcb3bd0a02c7" && quest.status == 4) {
								hasKnightQuest = true;
							}
							if (["67768936fa281ca31708b17c"].includes(quest.qid) && quest.status == 4) {
								hasBigPipeQuest = true;
							}

							if (["67768a41fa281ca31708b182"].includes(quest.qid) && quest.status == 4) {
								hasBirdEyeQuest = true;
							}
						});

						// low standing will result in the rest of the goons not being available
						if (profile.TradersInfo["67768b19fa281ca31708b187"].standing < 0.5) {
							hasBigPipeQuest = false;
							hasBirdEyeQuest = false;
						}

						if (!hasKnightQuest) {
							list.Friends = list.Friends.filter(friend => friend._id != knightFriend.getChatBot()._id);
						}
						if (!hasBigPipeQuest) {
							list.Friends = list.Friends.filter(friend => friend._id != bigPipeFriend.getChatBot()._id);
						}

						if (!hasBirdEyeQuest) {
							list.Friends = list.Friends.filter(friend => friend._id != birdEyeFriend.getChatBot()._id);
						}
					} catch (e) {
						this.Logger.error("friendlyPMC: Error in friend list: " + e);
					}

					return httpResponseUtil.getBody(list);
				}),
				new RouteAction("/client/match/raid/ready", async (url: string, info: any, sessionID: string, output: string): Promise<any> => {
					return httpResponseUtil.getBody(true);
				}),
				new RouteAction("/client/match/raid/not-ready", async (url: string, info: any, sessionID: string, output: string): Promise<any> => {
					return httpResponseUtil.getBody(true);
				}),
				new RouteAction("/client/match/group/pitstatus", async (url: string, info: { Players: string[] }, sessionID: string, output: string) => {
					clearTimeout(groupStatus[sessionID]);
					groupStatus[sessionID] = setTimeout(() => {
						const knightFriend = this.knightBot;
						const bigPipeFriend = container.resolve<BigPipeChatBot>("BigPipeChatBot");
						const birdEyeFriend = container.resolve<BirdEyeChatBot>("BirdEyeChatBot");
						knightFriend.currentGroup = info.Players;
						bigPipeFriend.currentGroup = info.Players;
						birdEyeFriend.currentGroup = info.Players;
					});

					return httpResponseUtil.emptyResponse();
				}),
			],
			"custom-static-friendly-pmc"
		);

		container.register<PitQuestItemEventRouter>("PitQuestItemEventRouter", { useClass: PitQuestItemEventRouter }, { lifecycle: Lifecycle.Singleton });
		this.questItemEvent = container.resolve<PitQuestItemEventRouter>("PitQuestItemEventRouter");

		const folder = path.basename(path.dirname(__dirname));
		this.modFolderName = folder;
		this.knightTrader = new KnightTrader(folder, preSptModLoader, imageRouter, traderConfig, ragfairConfig, jsonUtil);
		//this.generalTrader = new GeneralTrader(folder, preSptModLoader, imageRouter, traderConfig, ragfairConfig, jsonUtil);
	}

	postDBLoad(container: DependencyContainer) {
		const configServer = container.resolve<ConfigServer>("ConfigServer");

		const Bots = configServer.getConfig<IBotConfig>(ConfigTypes.BOT);

		const databaseServer = container.resolve<DatabaseServer>("DatabaseServer");
		this.databaseServer = databaseServer;
		const databaseImporter = container.resolve<ImporterUtil>("ImporterUtil");

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

		this.Bots = Bots;

		this.botsTable = tables.bots;

		this.myDBFolder = `${this.preSptModLoader.getModPath(this.modFolderName)}database/`;
		this.mydb = databaseImporter.loadRecursive(this.myDBFolder);
		this.questItemEvent.ModDB(this.mydb, this.myDBFolder);

		const locations = tables.locations;
		for (let k in locations) {
			const loc: (typeof locations)["bigmap"] = locations[k];
			if (!loc.base || loc.base.Name == "Private Sector" || loc.base.Name == "Terminal" || loc.base.Name == "Town" || loc.base.Name == "Suburbs" || loc.base.Name == "Arena") continue;

			if (!loc.base.BotLocationModifier?.AdditionalHostilitySettings) continue;
			this._hostilitySettings[k] = objectCopy(loc.base.BotLocationModifier.AdditionalHostilitySettings);
		}

		// add new items to the database
		for (let item of this.mydb.pit_items.items) {
			tables.templates.items[item._id] = item;
			let handbook = this.mydb.pit_items.handbook.find((i: any) => i.Id == item._id);
			tables.templates.handbook.Items.push(handbook);
			if (this.mydb.pit_items.locales) {
				let lang = this.mydb.pit_items.locales.find((i: any) => i.Id == item._id);
				if (lang.Clone) {
					const locales = Object.values(tables.locales.global) as Record<string, string>[];
					for (const locale of locales) {
						locale[`${item._id} Name`] = locale[lang.Clone + " Name"];
						locale[`${item._id} ShortName`] = locale[lang.Clone + " ShortName"];
						locale[`${item._id} Description`] = locale[lang.Clone + " Description"];
					}
				}
			}
		}
		for (const preset in this.mydb.globals.ItemPresets) {
			tables.globals.ItemPresets[preset] = this.mydb.globals.ItemPresets[preset];
			const locales = Object.values(tables.locales.global) as Record<string, string>[];
			for (const locale of locales) {
				locale[preset] = "";
			}
		}

		// add new traders to the database
		this.knightTrader.AddToDb(tables);
		//this.generalTrader.AddToDb(tables); // not for 3.9

		// add new chat bots to the database
		container.register<KnightChatBot>("KnightChatBot", KnightChatBot, {
			lifecycle: Lifecycle.Singleton,
		});
		container.register<BigPipeChatBot>("BigPipeChatBot", BigPipeChatBot, {
			lifecycle: Lifecycle.Singleton,
		});
		container.register<BirdEyeChatBot>("BirdEyeChatBot", BirdEyeChatBot, {
			lifecycle: Lifecycle.Singleton,
		});

		const knightBot = container.resolve<KnightChatBot>("KnightChatBot");
		knightBot.SetLang(this.lang);
		this.knightBot = knightBot;

		const bigPipeBot = container.resolve<BigPipeChatBot>("BigPipeChatBot");
		bigPipeBot.SetLang(this.lang);
		this.bigPipeBot = bigPipeBot;

		const birdEyeBot = container.resolve<BirdEyeChatBot>("BirdEyeChatBot");
		birdEyeBot.SetLang(this.lang);
		this.birdEyeBot = birdEyeBot;

		container.resolve<DialogueController>("DialogueController").registerChatBot(knightBot);
	}

	private _makeFriendlyOrHostile(diff: IDifficultyCategories, pmcType: string) {
		const is_bad_guy = this.config.badGuy || false;

		if (is_bad_guy) {
			Object.assign(diff.Mind, {
				ENEMY_BY_GROUPS_PMC_PLAYERS: true,
				CAN_RECEIVE_PLAYER_REQUESTS_SAVAGE: false,
			});
			return diff;
		}

		const clearWrongEnemy = (mind: Record<string, string | number | boolean | string[]>, type: string) => {
			const enemyList = <string[]>mind.ENEMY_BOT_TYPES;

			const idx = enemyList.indexOf(type);
			if (idx > -1) enemyList.splice(idx, 1);

			const idxl = enemyList.indexOf(type.toLowerCase());
			if (idxl > -1) enemyList.splice(idxl, 1);
		};

		const is_friendly = this.config.friendlyPMC || false;

		pmcType = pmcType.toLowerCase();

		// force the friendly mind here as some mods may overwrite things
		if (pmcType == "bear" || pmcType == "usec" || pmcType == "sptbear" || pmcType == "sptusec" || pmcType == "pmcbear" || pmcType == "pmcusec") {
			Object.assign(diff.Mind, {
				DEFAULT_ENEMY_BEAR: is_friendly && (pmcType == "bear" || pmcType == "sptbear" || pmcType == "pmcbear") ? false : diff.Mind.DEFAULT_ENEMY_BEAR,
				DEFAULT_ENEMY_SAVAGE: true,
				DEFAULT_ENEMY_USEC: is_friendly && (pmcType == "usec" || pmcType == "sptusec" || pmcType == "pmcusec") ? false : diff.Mind.DEFAULT_ENEMY_USEC,
				DEFAULT_BEAR_BEHAVIOUR: is_friendly && (pmcType == "bear" || pmcType == "sptbear" || pmcType == "pmcbear") ? "Neutral" : diff.Mind.DEFAULT_BEAR_BEHAVIOUR,
				DEFAULT_SAVAGE_BEHAVIOUR: "AlwaysEnemies",
				DEFAULT_USEC_BEHAVIOUR: is_friendly && (pmcType == "usec" || pmcType == "sptusec" || pmcType == "pmcusec") ? "Neutral" : diff.Mind.DEFAULT_BEAR_BEHAVIOUR,
				CAN_RECIVE_PLAYER_REQUESTS: is_friendly ? true : diff.Mind.CAN_RECIVE_PLAYER_REQUESTS,
				CAN_RECEIVE_PLAYER_REQUESTS: is_friendly ? true : diff.Mind.CAN_RECEIVE_PLAYER_REQUESTS,
				CAN_RECEIVE_PLAYER_REQUESTS_USEC: is_friendly ? true : diff.Mind.CAN_RECEIVE_PLAYER_REQUESTS_USEC,
				CAN_RECEIVE_PLAYER_REQUESTS_BEAR: is_friendly ? true : diff.Mind.CAN_RECEIVE_PLAYER_REQUESTS_BEAR,
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

			if (is_friendly) {
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
				ENEMY_BY_GROUPS_PMC_PLAYERS: !is_friendly || is_bad_guy,
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
				DEFAULT_USEC_BEHAVIOUR: "Neutral",
				DEFAULT_BEAR_BEHAVIOUR: "Neutral",
				DEFAULT_ENEMY_BEAR: false,
				DEFAULT_ENEMY_USEC: false,
			});
		}

		return diff;
	}

	originalgetPmcDifficultySettings: BotDifficultyHelper["getDifficultySettings"];
	/** Overwrite get difficulty method to patch the friendly/hostile settings */
	getPmcDifficultySettings(pmcType: "bear" | "usec", difficulty: string): any {
		const result = this.originalgetPmcDifficultySettings(pmcType, difficulty);
		return this._makeFriendlyOrHostile(result, pmcType);
	}

	originalgetBotDifficulty: BotController["getBotDifficulty"];
	/** Overwrite get difficulty method to patch the friendly/hostile settings */
	getBotDifficulty(type: string, difficulty: string, raidConfig?: IGetRaidConfigurationRequestData, ignoreRaidSettings?: boolean): any {
		let result = this.originalgetBotDifficulty(type, difficulty, raidConfig, ignoreRaidSettings);

		return this._makeFriendlyOrHostile(result, type);
	}

	originalGenerateBot: BotGenerator["generateBot"];
	generateBot(sessionId: string, bot: IBotBase, botJsonTemplate: IBotType, botGenerationDetails: IBotGenerationDetails) {
		const role = botGenerationDetails.role.toLowerCase();
		// ensure goons have high chance of meds
		if (role == "followerbirdeye" || role == "followerbigpipe" || role == "bossknight") {
			botJsonTemplate.generation.items.healing.weights = {
				"0": 0,
				"1": 2,
				"2": 6,
			};
		}

		const result = this.originalGenerateBot(sessionId, bot, botJsonTemplate, botGenerationDetails);

		return result;
	}

	originalGetOtherProfile: ProfileController["getOtherProfile"];
	getOtherProfile(sessionId: string, request: IGetOtherProfileRequest) {
		if (request.accountId == this.knightBot.getChatBot().aid.toString()) return this.knightBot.PlayerVisualRepresentation(sessionId);
		else if (request.accountId == this.bigPipeBot.getChatBot().aid.toString()) return this.bigPipeBot.PlayerVisualRepresentation(sessionId);
		else if (request.accountId == this.birdEyeBot.getChatBot().aid.toString()) return this.birdEyeBot.PlayerVisualRepresentation(sessionId);
		return this.originalGetOtherProfile(sessionId, request);
	}

	originalHandleItemEvent: QuestItemEventRouter["handleItemEvent"];
	handleItemEvent(eventAction: string, pmcData: IPmcData, body: any, sessionID: string) {
		this.questItemEvent.handleItemEvent(eventAction, pmcData, body, sessionID);
		return this.originalHandleItemEvent(eventAction, pmcData, body, sessionID);
	}

	originalLauncherLogin: LauncherController["login"];
	launcherLogin(info: ILoginRequestData) {
		const profile = this.originalLauncherLogin(info);
		for (const sessionID in this.profileHelper.getProfiles()) {
			const profile = this.profileHelper.getPmcProfile(sessionID);
			profile.Quests.forEach(quest => {
				if (quest.status == 2) {
					this.questItemEvent.handleItemEvent("QuestAccept", profile, { qid: quest.qid }, sessionID);
				}
			});
		}
		return profile;
	}
}

export const mod = new friendlyPMC();
