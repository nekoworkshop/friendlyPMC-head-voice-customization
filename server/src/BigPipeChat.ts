import { injectable } from "tsyringe";
import { IWsGroupMatchInviteAccept, KnightChatBot } from "./KnightChat";
import { NotificationEventType } from "@spt/models/enums/NotificationEventType";
import { MessageType } from "@spt/models/enums/MessageType";
import { ISendMessageDetails } from "@spt/models/spt/dialog/ISendMessageDetails";
import { IUserDialogInfo } from "@spt/models/eft/profile/ISptProfile";
import { MemberCategory } from "@spt/models/enums/MemberCategory";

@injectable()
export class BigPipeChatBot extends KnightChatBot {
	getChatBot(): IUserDialogInfo {
		return {
			_id: "followerBigPipe",
			aid: 1113580,
			Info: {
				Level: 99,
				MemberCategory: MemberCategory.SHERPA,
				SelectedMemberCategory: MemberCategory.SHERPA,
				Nickname: "BigPipe",
				Side: "Usec",
			},
		};
	}

	public SetLang(lang: { [key: string]: any }) {
		super.SetLang(lang);
		this.chatHelp.joinRaid = lang.chatHelp.joinRaid.BigPipe;
		this.chatResponses.joinRaid = lang.chatResponses.joinRaid.BigPipe;
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
			let messageText = this.randUtil.getArrayValue(this.chatResponses.joinRaid);
			const details: ISendMessageDetails = {
				recipientId: sessionId,
				sender: MessageType.USER_MESSAGE,
				senderDetails: profile,
				//@prettier-ignore
				messageText: messageText,
			};
			// Get dialog, create if doesn't exist
			const senderDialog = this.mailSendService["getDialog"](details);

			senderDialog.Users = senderDialog.Users || [];
			senderDialog.Users.push(profile); // insertion is here

			// Flag dialog as containing a new message to player
			senderDialog.new++;

			// Craft message
			const message = this.mailSendService["createDialogMessage"](senderDialog._id, details);

			// Add message to dialog
			senderDialog.messages.push(message);

			// Send message off to player so they get it in client
			const notificationMessage = this.mailSendService["notifierHelper"].createNewMessageNotification(message);
			this.mailSendService["notificationSendHelper"].sendMessage(details.recipientId, notificationMessage);
		}, 1000);
	}
}
