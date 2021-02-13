#include "QuestManager.h"

QuestManager::QuestManager(){

	this->numQuests = 0;
	this->quests = std::vector<Quest*>();

}

QuestManager::~QuestManager(){

	for(Quest* quest : this->quests){
		delete(quest);
	}

	this->quests.clear();
	this->quests.shrink_to_fit();

}

void QuestManager::AddQuest(Quest* quest){

	quest->SetQuestID(this->numQuests);
	this->numQuests += 1;
	this->quests.push_back(quest);

	std::sort(this->quests.begin(), this->quests.end(), [](Quest* a, Quest* b){ return a->GetQuestID()<b->GetQuestID(); });

}

Quest* QuestManager::GetQuestAtIndex(int idx){

	if(idx < 0 || idx >= this->numQuests){
		return NULL;
	}

	return this->quests[idx];
}

void QuestManager::RemoveQuestAtIndex(int idx){
	if(idx < 0 || idx >= this->numQuests){
		return;
	}

	this->quests.erase(this->quests.begin() + idx);
	this->quests.shrink_to_fit();

	int count = 0;
	for(Quest* quest : this->quests){
		quest->SetQuestID(count);
		count += 1;
	}
}

void QuestManager::PromoteQuest(int idx){
	if(idx > 0){

		Quest* above = this->quests[idx - 1];
		this->quests[idx - 1] = this->quests[idx];
		this->quests[idx] = above;

		above->SetQuestID(idx);
		this->quests[idx-1]->SetQuestID(idx-1);
		std::sort(this->quests.begin(), this->quests.end(), [](Quest* a, Quest* b){ return a->GetQuestID()<b->GetQuestID(); });

	}
}

void QuestManager::DemoteQuest(int idx){
	if(idx < this->numQuests - 1){

		Quest* below = this->quests[idx + 1];
		this->quests[idx + 1] = this->quests[idx];
		this->quests[idx] = below;

		below->SetQuestID(idx);
		this->quests[idx+1]->SetQuestID(idx+1);
		std::sort(this->quests.begin(), this->quests.end(), [](Quest* a, Quest* b){ return a->GetQuestID()<b->GetQuestID(); });

	}
}


std::string QuestManager::GetQuestsText(){

	std::string text = std::to_string(this->numQuests) + "\n\n";

	for(Quest* quest : this->quests){
		text.append(quest->GetQuestFragment() + "\n");
	}

	return text;
}

void QuestManager::ReadQuestsText(std::string text){

	std::vector<std::string> lines = splitStrings(text, '\n');

	int numQuests = std::stoi(lines[0]);

	int currentLine = 2;
	for(int quests = 0; quests < numQuests; quests++){


		std::vector<std::string> quest_values = splitStrings(lines[currentLine], ' ');
		int numBlocks = std::stoi(quest_values[2]);

		Quest* newQuest = new Quest(quest_values[1]);

		currentLine++;

		for(int blocks = 0; blocks < numBlocks; blocks++){

			std::vector<std::string> quest_blocks = splitStrings(lines[currentLine], ' ');

			QuestActionBlock* qab = new QuestActionBlock();
			qab->SetOperand(quest_blocks[2]);

			newQuest->AddBlock(qab);

			int numActions = std::stoi(quest_blocks[1]);

			currentLine++;
			for(int ac = 0; ac < numActions; ac++){

				std::vector<std::string> action_values = splitStrings(lines[currentLine], ' ');

				QuestAction* qa = new QuestAction(action_values[0]);


				qab->AddAction(qa);

				currentLine++;
			}

		}
		currentLine++;

		AddQuest(newQuest);

	}


}

std::vector<std::string> QuestManager::splitStrings(std::string word, char split){
	std::vector<std::string> words;

	int init_idx = -1;

	for(int i = 0; i < word.length(); i++){

		if(word.at(i) == split){
			words.push_back(word.substr(init_idx + 1, i - init_idx - 1));
			init_idx = i;
		}

	}

	words.push_back(word.substr(init_idx + 1, word.length() - init_idx - 1));

	return words;

}