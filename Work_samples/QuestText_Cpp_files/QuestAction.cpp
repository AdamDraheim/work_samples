#include "QuestAction.h"

QuestAction::QuestAction(std::string keyword){
	this->keyword = keyword;
	SetRTV("");
	SetSubj("");
	setJump(0);
	SetDesc("");
	SetFlags(std::list<std::string>());
}

std::string QuestAction::GetActionFragment(){
	std::string action_fragment = this->keyword + ' ';

	if(this->requisite_threshold_value != ""){
		action_fragment.append(this->requisite_threshold_value + " ");
	}

	action_fragment.append(this->subject_specification + " ");

	std::string jmp = std::to_string(this->jump_address);
	action_fragment.append("-jump<" + std::string(jmp) + "> ");
	action_fragment.append("desc<" + this->description + "> ");

	for(std::string flag : this->flags){
		action_fragment.append(flag + " ");
	}

	action_fragment.append("\n");

	return action_fragment;

}

void QuestAction::SetRTV(std::string rtv){
	this->requisite_threshold_value = rtv;
}

void QuestAction::SetSubj(std::string subj){
	this->subject_specification = subj;
}

void QuestAction::setJump(int jump){
	this->jump_address = jump;
}

void QuestAction::SetDesc(std::string desc){
	this->description = desc;
}
void QuestAction::SetFlags(std::list<std::string> flags){
	this->flags = flags;
}