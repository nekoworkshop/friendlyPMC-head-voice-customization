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

import { PitQuestItemEventRouter, Quests } from "./Quests";
import { QuestItemEventRouter } from "@spt/routers/item_events/QuestItemEventRouter";
import { IPmcData } from "@spt/models/eft/common/IPmcData";

import { BigPipeChatBot } from "./BigPipeChat";
import { BirdEyeChatBot } from "./BirdEyeChat";

import { IGetOtherProfileRequest } from "@spt/models/eft/profile/IGetOtherProfileRequest";

import { GameCallbacks } from "@spt/callbacks/GameCallbacks";
import { IEmptyRequestData } from "@spt/models/eft/common/IEmptyRequestData";

import { objectCopy, objectForEach } from "./Utils";
import { IGetRaidConfigurationRequestData } from "@spt/models/eft/match/IGetRaidConfigurationRequestData";
import { IAdditionalHostilitySettings } from "@spt/models/eft/common/ILocationBase";
import { HashUtil } from "@spt/utils/HashUtil";

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
	hashUtil: HashUtil;
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
		this.hashUtil = container.resolve<HashUtil>("HashUtil");

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
		// patch handleItemEvent so that we can handle our custom events
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
		// patch launcher login to handle our custom quests
		this.gameStart = this.gameStart.bind(this);
		container.afterResolution(
			"GameCallbacks",
			(_t, result: GameCallbacks) => {
				if (!this.originalGameStart) {
					this.originalGameStart = result.gameStart.bind(result);

					result.gameStart = this.gameStart;
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

					return httpResponseUtil.emptyResponse();
				}),
				new RouteAction("/client/game/bot/followergenerate", (url: string, info: { Info: IGenerateBotsRequestData; Preset?: string; Custom?: { Body?: string; Feet?: string; Equipment?: string; Nickname?: string; English?: boolean; Voice?: string } }, sessionID: string, output: string): any => {
					const pmcProfile = profileHelper.getPmcProfile(sessionID);
					const playerProfile = profileHelper.getFullProfile(sessionID);

					const custom = info.Custom;

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
						if (this.config.armbands && botGenerationDetails.isPmc) {
							botJsonTemplateClone.chances.equipment.ArmBand = 100;

							if (pmcProfile.Info.Side.toLowerCase() == "bear") {
								botJsonTemplateClone.inventory.equipment.ArmBand[ItemTpl.ARMBAND_RED] = 1;
							} else {
								botJsonTemplateClone.inventory.equipment.ArmBand[ItemTpl.ARMBAND_BLUE] = 1;
							}
						}

						if (botGenerationDetails.isPmc && custom) {
							if (custom.Body) {
								botJsonTemplateClone.appearance.body = {};
								botJsonTemplateClone.appearance.body[custom.Body] = 1;
								preparedBotBase.Customization.Body = custom.Body;
							}
							if (custom.Feet) {
								botJsonTemplateClone.appearance.feet = {};
								botJsonTemplateClone.appearance.feet[custom.Feet] = 1;
								preparedBotBase.Customization.Feet = custom.Feet;
							}

							if (custom.Nickname) {
								botJsonTemplateClone.firstName = [custom.Nickname];
								botGenerationDetails.playerName = custom.Nickname;
								preparedBotBase.Info.Nickname = custom.Nickname;
							}

							if (custom.Voice && pmcProfile.Info.Side.toLowerCase() == "bear") {
								botJsonTemplateClone.appearance.voice = {};
								botJsonTemplateClone.appearance.voice["Bear_1_Eng"] = 1;
								botJsonTemplateClone.appearance.voice["Bear_2_Eng"] = 1;
								preparedBotBase.Info.Voice = this.randomUtil.getArrayValue(["Bear_1_Eng", "Bear_2_Eng"]);
							}
						}

						const bot = botGenerator["generateBot"](sessionID, preparedBotBase, botJsonTemplateClone, botGenerationDetails);

						if (botGenerationDetails.isPmc) {
							// force name change
							if (custom?.Nickname) bot.Info.Nickname = custom.Nickname;
							// equipment change
							if (custom?.Equipment) {
								const equipment = playerProfile.userbuilds.equipmentBuilds?.find(e => e.Name == custom.Equipment);
								if (equipment) {
									let eid = equipment.Items[0]._id;
									let clonedBuild = objectCopy(equipment.Items);
									let newid = this.hashUtil.generate();
									clonedBuild[0]._id = newid;
									clonedBuild.forEach(item => {
										if (item.parentId == eid) {
											item.parentId = newid;
										}
									});

									// go through all the items and ensure their ids are unique in the clonedBuild, exclude the first item
									// check the parent id of any item to update it to the new id should the item have a parent an item for which we just changed the id
									clonedBuild.forEach(item => {
										if (item._id == newid) return;
										const _pid = item._id;
										item._id = this.hashUtil.generate();
										clonedBuild.forEach(i => {
											if (i.parentId == _pid) i.parentId = item._id;
										});
									});

									bot.Inventory.equipment = newid;

									const botSpecialItems = bot.Inventory.items.filter(item => {
										if (!item.slotId) return false;
										const id = item.slotId.toLowerCase();
										return id.toLowerCase().includes("dogtag") || id.includes("secured") || item.slotId.includes("special");
									});

									bot.Inventory.items = clonedBuild
										.filter(item => {
											if (!item.slotId) return true;
											return !item.slotId.includes("SpecialSlot") && !item.slotId.includes("SecuredContainer");
										})
										.concat(
											botSpecialItems.map(item => {
												if (item.slotId.includes("SpecialSlot")) {
													let id = clonedBuild.find(i => i.slotId == "Pockets")?._id;
													if (!id) item.parentId = newid;
													else item.parentId = id;
												} else item.parentId = newid;

												return item;
											})
										);
								}
							}
						}

						conditionPromises.push(bot);
					}
					const res = httpResponseUtil.getBody(conditionPromises);

					return res;
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
		// get the language files - english is default
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

	originalGameStart: GameCallbacks["gameStart"];
	gameStart(url: string, info: IEmptyRequestData, sessionID: string) {
		const result = this.originalGameStart(url, info, sessionID);

		const profile = this.profileHelper.getPmcProfile(sessionID);
		if (!profile || !profile.Quests) return result;

		profile.Quests.forEach(quest => {
			let id = quest.qid;
			if (Quests[id]) {
				if (quest.status == 2) {
					this.questItemEvent.handleItemEvent("QuestAccept", profile, { qid: id }, sessionID);
				}
			}
		});

		return result;
	}
}

export const mod = new friendlyPMC();
