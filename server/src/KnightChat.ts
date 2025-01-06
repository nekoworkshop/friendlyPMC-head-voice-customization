import { IDialogueChatBot } from "@spt/helpers/Dialogue/IDialogueChatBot";
import { NotificationSendHelper } from "@spt/helpers/NotificationSendHelper";
import { ISendMessageRequest } from "@spt/models/eft/dialog/ISendMessageRequest";
import { MemberCategory } from "@spt/models/enums/MemberCategory";
import { MailSendService } from "@spt/services/MailSendService";
import { inject, injectable } from "tsyringe";

import { NotificationEventType } from "@spt/models/enums/NotificationEventType";

import { IWsNotificationEvent } from "@spt/models/eft/ws/IWsNotificationEvent";
import { IGroupCharacter } from "@spt/models/eft/match/IGroupCharacter";

import { IUserDialogInfo } from "@spt/models/eft/profile/ISptProfile";
import { MessageType } from "@spt/models/enums/MessageType";
import { HashUtil } from "@spt/utils/HashUtil";
import { ProfileHelper } from "@spt/helpers/ProfileHelper";
import { RandomUtil } from "@spt/utils/RandomUtil";
import { BotGenerator } from "@spt/generators/BotGenerator";
import { BotController } from "@spt/controllers/BotController";

import { IGetOtherProfileResponse } from "@spt/models/eft/profile/IGetOtherProfileResponse";
import { ItemTpl } from "@spt/models/enums/ItemTpl";

export interface IWsGroupMatchInviteAccept extends IWsNotificationEvent, IGroupCharacter {}

@injectable()
export class KnightChatBot implements IDialogueChatBot {
	protected _botRole = "bossKnight";

	private _StringFormat(str: string, ...values: string[]) {
		return str.replace(/\{(\d+)\}/g, function (match, number) {
			return typeof values[number] != "undefined" && values[number] !== null ? values[number] : match;
		});
	}
	//prettier-ignore
	public constructor(
        @inject("MailSendService") protected mailSendService: MailSendService, 
        @inject("ProfileHelper") protected profileHelper: ProfileHelper, 
        @inject("NotificationSendHelper") protected notificationSendHelper: NotificationSendHelper, 
        @inject("HashUtil") protected hashUtil: HashUtil,
        @inject("RandomUtil") protected randUtil: RandomUtil,
        @inject("BotGenerator") protected botGenerator: BotGenerator,
        @inject("BotController") protected botController: BotController
    ) {
        
    }

	public SetLang(lang: { [key: string]: any }) {
		this.chatHelp.joinRaid = lang.chatHelp.joinRaid.Knight;
		this.chatHelp.giveWeapon = this._StringFormat(lang.chatHelp.giveWeapon, "'" + this.chatCommands.giveWeapon.join("' or '") + "'");

		this.chatResponses.giveWeapon = lang.chatResponses.giveWeapon;
		this.chatResponses.joinRaid = lang.chatResponses.joinRaid.Knight;
		this.chatResponses.help = lang.chatResponses.help;
	}

	"chatCommands" = {
		joinRaid: ["/join", "/teamup"],
		giveWeapon: ["/take", "/use "],
	};
	chatHelp = {
		joinRaid: "",
		giveWeapon: "",
	};
	chatResponses = {
		help: "",
		joinRaid: [""],
		giveWeapon: "",
	};

	currentGroup: string[] = [];

	getChatBot(): IUserDialogInfo {
		return {
			_id: "677c4e0cc7a538c4210d4d47",
			aid: 1113579,
			Info: {
				Level: 99,
				MemberCategory: MemberCategory.SHERPA,
				SelectedMemberCategory: MemberCategory.SHERPA,
				Nickname: "Knight",
				Side: "Usec",
			},
		};
	}

	public PlayerVisualRepresentation(sessionId: string): IGetOtherProfileResponse {
		const pmcProfile = this.profileHelper.getPmcProfile(sessionId);
		const raidSettings = this.botController["getMostRecentRaidSettings"]();
		const botGenerationDetails = this.botController["getBotGenerationDetailsForWave"](
			{
				Role: this._botRole,
				Limit: 1,
				Difficulty: "hard",
			},
			pmcProfile,
			false,
			raidSettings,
			1,
			false
		);
		const preparedBotBase = this.botGenerator["getPreparedBotBase"](
			botGenerationDetails.eventRole ?? botGenerationDetails.role, // Use eventRole if provided,
			pmcProfile.Info.Side,
			botGenerationDetails.botDifficulty
		);

		const botRole = botGenerationDetails.role;
		const botJsonTemplateClone = this.botController["cloner"].clone(this.botController["botHelper"].getBotTemplate(botRole));

		botGenerationDetails.botRelativeLevelDeltaMax = 1;
		botGenerationDetails.botRelativeLevelDeltaMin = 1;

		const result = this.botGenerator["generateBot"](sessionId, preparedBotBase, botJsonTemplateClone, botGenerationDetails);

		const info = Object.assign(this.getChatBot(), { GameVersion: "edge_of_darkness" });

		return {
			id: info._id,
			aid: info.aid,
			info: {
				nickname: info.Info.Nickname,
				side: info.Info.Side,
				experience: result.Info.Experience,
				memberCategory: info.Info.MemberCategory,
				bannedState: pmcProfile.Info.BannedState,
				bannedUntil: pmcProfile.Info.BannedUntil,
				registrationDate: pmcProfile.Info.RegistrationDate,
			},
			customization: {
				head: result.Customization.Head,
				body: result.Customization.Body,
				feet: result.Customization.Feet,
				hands: result.Customization.Hands,
			},
			skills: pmcProfile.Skills,
			equipment: {
				// Default inventory tpl
				Id: result.Inventory.items.find(item => item._tpl === ItemTpl.INVENTORY_DEFAULT)._id,
				Items: result.Inventory.items,
			},
			achievements: pmcProfile.Achievements,
			favoriteItems: [],
			pmcStats: {
				eft: {
					totalInGameTime: pmcProfile.Stats.Eft.TotalInGameTime,
					overAllCounters: pmcProfile.Stats.Eft.OverallCounters,
				},
			},
			scavStats: {
				eft: {
					totalInGameTime: pmcProfile.Stats.Eft.TotalInGameTime,
					overAllCounters: pmcProfile.Stats.Eft.OverallCounters,
				},
			},
		};
	}

	public handleMessage(sessionId: string, request: ISendMessageRequest): string {
		if (request.text == "/help") {
			setTimeout(() => {
				this.mailSendService.sendUserMessageToPlayer(sessionId, this.getChatBot(), this.chatResponses.help);
				setTimeout(() => {
					let message = this.chatHelp.joinRaid;
					//message += "\n\n" + this.chatHelp.giveWeapon;
					this.mailSendService.sendUserMessageToPlayer(sessionId, this.getChatBot(), message);
				}, 1000);
			}, 1000);
			return request.dialogId;
		}

		return request.dialogId;
	}

	public acceptInvite(sessionId: string) {
		const profile = this.getChatBot();

		const notification: IWsGroupMatchInviteAccept = {
			type: NotificationEventType.GROUP_MATCH_INVITE_ACCEPT,
			eventId: this.hashUtil.generate(),
			Info: profile.Info,
			_id: profile._id,
			aid: profile.aid,
			isLeader: false,
			isReady: true,
		};
		this.notificationSendHelper.sendMessage(sessionId, notification);

		setTimeout(() => {
			let message = this.randUtil.getArrayValue(this.chatResponses.joinRaid);
			this.mailSendService.sendMessageToPlayer({
				recipientId: sessionId,
				sender: MessageType.NPC_TRADER,
				//@ts-ignore
				trader: "67768b19fa281ca31708b187",
				messageText: message,
			});
		}, 1000);
	}
}
