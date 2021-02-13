#include "QuestActionBlock.h"
#include<stdio.h>
#include <iostream>

QuestActionBlock::QuestActionBlock(){
	this->actions = std::vector<QuestAction*>();
	this->numQuestActions = 0;
	this->blockID = 0;
	this->operand = "AND";
}

QuestActionBlock::~QuestActionBlock(){

	for(QuestAction* qa : this->actions){
		delete(qa);
	}

	this->actions.clear();
	this->actions.shrink_to_fit();
}

std::string QuestActionBlock::GetBlockFragment(){

	std::string fragment = "";

	std::string id = std::to_string(this->blockID);
	std::string nActions = std::to_string(this->numQuestActions);

	fragment.append(id + " " + nActions + " " + this->operand + "\n");

	for(QuestAction* action : this->actions){
		fragment.append(action->GetActionFragment());
	}

	return fragment;

}

void QuestActionBlock::SetBlockID(int id){
	this->blockID = id;
}

void QuestActionBlock::AddAction(QuestAction* action){
	this->numQuestActions += 1;
	this->actions.push_back(action);
}

QuestAction* QuestActionBlock::GetActionAtIndex(int idx){
	
	if(this->numQuestActions <= idx || idx < 0){
		return NULL;
	}

	return this->actions[idx];

}

void QuestActionBlock::RemoveActionAtIndex(int idx){

	if(idx >= this->numQuestActions || idx < 0){
		return;
	}

	this->numQuestActions -= 1;
	this->actions.erase(this->actions.begin() + idx);
	this->actions.shrink_to_fit();

}

int QuestActionBlock::GetBlockID(){
	return this->blockID;
}

void QuestActionBlock::SetOperand(std::string operand){
	this->operand = operand;
}

void QuestActionBlock::SetBlockId(int idx){
	this->blockID = idx;
}
