import { IDialogueChatBot } from "@spt/helpers/Dialogue/IDialogueChatBot";
import { NotificationSendHelper } from "@spt/helpers/NotificationSendHelper";
import { ISendMessageRequest } from "@spt/models/eft/dialog/ISendMessageRequest";
import { MemberCategory } from "@spt/models/enums/MemberCategory";
import { ILogger } from "@spt/models/spt/utils/ILogger";
import { MailSendService } from "@spt/services/MailSendService";
import { inject, injectable } from "tsyringe";

import { NotificationEventType } from "@spt/models/enums/NotificationEventType";
import { IWsChatMessageReceived } from "@spt/models/eft/ws/IWsChatMessageReceived";

import { IWsNotificationEvent } from "@spt/models/eft/ws/IWsNotificationEvent";
import { IGroupCharacter } from "@spt/models/eft/match/IGroupCharacter";

import { Message } from "@spt/models/eft/profile/ISptProfile";
import { MessageType } from "@spt/models/enums/MessageType";
import { HashUtil } from "@spt/utils/HashUtil";
import { ProfileHelper } from "@spt/helpers/ProfileHelper";
import { RandomUtil } from "@spt/utils/RandomUtil";

interface IWsGroupMatchInviteAccept extends IWsNotificationEvent, IGroupCharacter {}

@injectable()
export class KnightChatBot implements IDialogueChatBot {
	private _StringFormat(str: string, ...values: string[]) {
		return str.replace(/\{(\d+)\}/g, function (match, number) {
			return typeof values[number] != "undefined" && values[number] !== null ? values[number] : match;
		});
	}

	public constructor(@inject("MailSendService") private mailSendService: MailSendService, @inject("ProfileHelper") private profileHelper: ProfileHelper, @inject("NotificationSendHelper") private notificationSendHelper: NotificationSendHelper, @inject("HashUtil") private hashUtil: HashUtil, @inject("RandomUtil") private randUtil: RandomUtil) {
		this.mailSendService = mailSendService;
		this.notificationSendHelper = notificationSendHelper;
	}

	public SetLang(lang: { [key: string]: any }) {
		this.chatHelp.joinRaid = this._StringFormat(lang.chatHelp.joinRaid, "'" + this.chatCommands.joinRaid.join("' or '") + "'");
		this.chatHelp.giveWeapon = this._StringFormat(lang.chatHelp.giveWeapon, "'" + this.chatCommands.giveWeapon.join("' or '") + "'");

		this.chatResponses.giveWeapon = lang.chatResponses.giveWeapon;
		this.chatResponses.joinRaid = lang.chatResponses.joinRaid;
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

	getChatBot() {
		return {
			_id: "bossKnight",
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
				trader: "friendlypmc-knight",
				messageText: message,
			});
		}, 1000);
	}
}
