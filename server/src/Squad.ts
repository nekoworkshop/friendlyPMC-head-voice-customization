import { DependencyContainer, inject } from "tsyringe";
import { DatabaseServer } from "@spt/servers/DatabaseServer";

import { BotDifficultyHelper } from "@spt/helpers/BotDifficultyHelper";
import { BotController } from "@spt/controllers/BotController";

import { IBotConfig } from "@spt/models/spt/config/IBotConfig";
import { IPmcConfig } from "@spt/models/spt/config/IPmcConfig";

import { ILogger } from "@spt/models/spt/utils/ILogger";

import { Difficulty, IBotType } from "@spt/models/eft/common/tables/IBotType";

import { LogTextColor } from "@spt/models/spt/logging/LogTextColor";

import { ILocations } from "@spt/models/spt/server/ILocations";

import { openZonesMap } from "./AOZExports";

import { ITraderConfig } from "@spt/models/spt/config/ITraderConfig";
import { TraderHelper } from "@spt/helpers/TraderHelper";
import { Traders } from "@spt/models/enums/Traders";
import { SetFreemanTrader } from "./Trader";

import { ImageRouter } from "@spt/routers/ImageRouter";
import type { PostSptModLoader } from "@spt/loaders/PostSptModLoader";

import { MailSendService } from "@spt/services/MailSendService";

import type { StaticRouterModService } from "@spt/services/mod/staticRouter/StaticRouterModService";

import path from "path";
import { RouteAction } from "@spt/di/Router";
import { HttpResponseUtil } from "@spt/utils/HttpResponseUtil";
import { IUserDialogInfo } from "@spt/models/eft/profile/ISptProfile";

import { RandomUtil } from "@spt/utils/RandomUtil";
import { BotGenerator } from "@spt/generators/BotGenerator";
import { IBotBase } from "@spt/models/eft/common/tables/IBotBase";
import { BotGenerationDetails } from "@spt/models/spt/bots/BotGenerationDetails";
import { HashUtil } from "@spt/utils/HashUtil";
import { ISendMessageDetails } from "@spt/models/spt/dialog/ISendMessageDetails";
import { MessageType } from "@spt/models/enums/MessageType";

class friendlyPMC {
	config = {
		sameSideHostile: false,
		armbands: true,
		englishBear: true,
	};

	Logger: ILogger;
	Bots: IBotConfig;
	mailSendService: MailSendService;

	originalgetPmcDifficultySettings: BotDifficultyHelper["getPmcDifficultySettings"];
	originalgetBotDifficulty: BotController["getBotDifficulty"];

	originalGetTraderById: TraderHelper["getTraderById"];

	originalGetValidTraderIdByEnumValue: TraderHelper["getValidTraderIdByEnumValue"];

	originalGenerateBot: BotGenerator["generateBot"];

	preSptLoad(container: DependencyContainer) {
		this.Logger = container.resolve("WinstonLogger");
		this.mailSendService = container.resolve("MailSendService");

		const botGenerator = container.resolve<BotGenerator>("BotGenerator");

		try {
			this.config = Object.assign(this.config, require("../config.json"));
		} catch (e) {
			this.Logger.error("friendlyPMC: something is wrong with the config, check below\n");
			console.error(e);
		}
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

		this.getTraderById = this.getTraderById.bind(this);
		container.afterResolution(
			"TraderHelper",
			(_t, result: TraderHelper) => {
				if (!this.originalGetTraderById) {
					this.originalGetTraderById = result.getTraderById.bind(result);
				}

				result.getTraderById = this.getTraderById;
			},
			{ frequency: "Always" }
		);

		this.getValidTraderIdByEnumValue = this.getValidTraderIdByEnumValue.bind(this);
		container.afterResolution(
			"TraderHelper",
			(_t, result: TraderHelper) => {
				if (!this.originalGetValidTraderIdByEnumValue) {
					this.originalGetValidTraderIdByEnumValue = result.getValidTraderIdByEnumValue.bind(result);
				}

				result.getValidTraderIdByEnumValue = this.getValidTraderIdByEnumValue;
			},
			{ frequency: "Always" }
		);

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

		const imageRouter: ImageRouter = container.resolve("ImageRouter");
		const modLoader: PostSptModLoader = container.resolve("PostSptModLoader");

		const folder = path.basename(path.dirname(__dirname));

		imageRouter.addRoute("/files/trader/avatar/general", `${modLoader.getModPath(folder)}/avatar/general.jpg`);

		// add a new router for handling items being given from the squad members
		const staticRouterModService = container.resolve<StaticRouterModService>("StaticRouterModService");
		const httpResponseUtil = container.resolve<HttpResponseUtil>("HttpResponseUtil");
		const randomUtil = container.resolve<RandomUtil>("RandomUtil");

		staticRouterModService.registerStaticRouter(
			"SquadItemsGiver",
			[
				new RouteAction("/singleplayer/returnitems", (url: string, info: any, sessionID: string, output: string): any => {
					const member = <IUserDialogInfo>info.member;

					const details: ISendMessageDetails = {
						recipientId: sessionID,
						sender: MessageType.USER_MESSAGE,
						senderDetails: member,
						//@prettier-ignore
						messageText: randomUtil.getArrayValue(["Here is your stuff. Uhm, anything in there for me? ", "Got your things right here. Where's my cut?", "Here is everything you gave me. So... we are splitting this, right?", "Here, this is everything you gave me.\nAnything in there for me?", "Here you go my friend, all the stuff you gave me.", "I got your stuff right here. Anything in there you can spare?"]),
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
			],
			"custom-static-squad-items-giver"
		);
	}

	postDBLoad(container: DependencyContainer) {
		const configServer: any = container.resolve("ConfigServer");

		const Bots: IBotConfig = configServer.getConfig("spt-bot");
		const PMCBOT: IPmcConfig = configServer.getConfig("spt-pmc");
		const Traders: ITraderConfig = configServer.getConfig("spt-trader");

		const databaseServer = container.resolve<DatabaseServer>("DatabaseServer");
		const tables = databaseServer.getTables();

		const globals = tables.globals;

		// same side hostile is being changed elsewhere - do this to avoid unwanted outcome
		PMCBOT.chanceSameSideIsHostilePercent = -1;
		// this is what actually makes bots follow you
		for (let lvl in globals.config.FenceSettings.Levels) {
			globals.config.FenceSettings.Levels[lvl].BotFollowChance = 100;
			globals.config.FenceSettings.Levels[lvl].ScavAttackSupport = true;
			globals.config.FenceSettings.Levels[lvl].BotApplySilenceChance = 100;
			globals.config.FenceSettings.Levels[lvl].BotGetInCoverChance = 100;
			globals.config.FenceSettings.Levels[lvl].BotHelpChance = 100;
			globals.config.FenceSettings.Levels[lvl].BotSpreadoutChance = 100;
			globals.config.FenceSettings.Levels[lvl].BotStopChance = 100;
			// stop spt* bosses from attacking you
			globals.config.FenceSettings.Levels[lvl].HostileBosses = false;
		}

		this.Bots = Bots;

		if (this.config.armbands) {
			this.Logger.logWithColor("friendlyPMC: Adding Armbands to bots...", LogTextColor.BLUE);

			const armbandColors: Record<string, string> = {
				blue: "5b3f3af486f774679e752c1f",
				green: "5b3f3b0186f774021a2afef7",
				red: "5b3f3ade86f7746b6b790d8e",
				white: "5b3f16c486f7747c327f55f7",
				yellow: "5b3f3b0e86f7746752107cda",
				purple: "5f9949d869e2777a0e779ba5",
			};

			for (const botType in tables.bots.types) {
				const bot = tables.bots.types[botType];
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
			tables.bots.types["bear"].appearance.voice = {
				Bear_1_Eng: 1,
				Bear_2_Eng: 1,
			};
		}

		// open all zones to the bots
		const locations: ILocations = tables.locations;
		for (const altLocation in openZonesMap) {
			locations[altLocation].base.OpenZones = openZonesMap[altLocation].join(",");
			this.Logger.info(`Opened ${locations[altLocation].base.OpenZones} for bots in ${locations[altLocation].base.Name} location`);
		}

		SetFreemanTrader(tables, Traders);
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
		pmcType = pmcType.toLowerCase();

		// force the friendly mind here as some mods may overwrite things
		if (pmcType == "bear" || pmcType == "usec" || pmcType == "sptbear" || pmcType == "sptusec" || pmcType == "pmcbear" || pmcType == "pmcusec") {
			Object.assign(diff.Mind, {
				DEFAULT_ENEMY_BEAR: pmcType == "usec" || pmcType == "sptusec" || pmcType == "pmcusec" || is_hostile,
				DEFAULT_ENEMY_SAVAGE: true,
				DEFAULT_ENEMY_USEC: pmcType == "bear" || pmcType == "sptbear" || pmcType == "pmcbear" || is_hostile,
				DEFAULT_BEAR_BEHAVIOUR: !is_hostile && (pmcType == "bear" || pmcType == "sptbear" || pmcType == "pmcbear") ? "Ignore" : "Attack",
				DEFAULT_SAVAGE_BEHAVIOUR: "Attack",
				DEFAULT_USEC_BEHAVIOUR: !is_hostile && (pmcType == "usec" || pmcType == "sptusec" || pmcType == "pmcusec") ? "Ignore" : "Attack",
				CAN_RECIVE_PLAYER_REQUESTS: !is_hostile,
				CAN_RECEIVE_PLAYER_REQUESTS: !is_hostile,
				CAN_RECEIVE_PLAYER_REQUESTS_USEC: !is_hostile,
				CAN_RECEIVE_PLAYER_REQUESTS_BEAR: !is_hostile,
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
				// do these do anything?

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
				ENEMY_BY_GROUPS_PMC_PLAYERS: is_hostile,
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

	getTraderById(traderId: string): Traders {
		if (traderId == "friendlypmc-return-loot") {
			return "FRIENDLYPMC" as any;
		}

		const result = this.originalGetTraderById(traderId);

		return result;
	}

	getValidTraderIdByEnumValue(traderEnumValue: Traders): string {
		if ((traderEnumValue as any) == "FRIENDLYPMC") {
			return "friendlypmc-return-loot";
		}
		const result = this.originalGetValidTraderIdByEnumValue(traderEnumValue);

		return result;
	}

	generateBot(sessionId: string, bot: IBotBase, botJsonTemplate: IBotType, botGenerationDetails: BotGenerationDetails) {
		const role = botGenerationDetails.role.toLowerCase();
		if (role == "followerbirdeye" || role == "followerbigpipe" || role == "bossknight") {
			botJsonTemplate.generation.items.healing.weights = {
				"0": 0,
				"1": 2,
				"2": 6,
			};

			this.Logger.info("FriendlyPMC:  Patching bot generation for " + role);
		}

		return this.originalGenerateBot(sessionId, bot, botJsonTemplate, botGenerationDetails);
	}
}

export const mod = new friendlyPMC();
