
#include "QuestActionBlock.cpp"

class Quest{

public:

	Quest();
	Quest(std::string name);
	~Quest();

	void AddBlock(QuestActionBlock* block);
	QuestActionBlock* GetBlockAtIdx(int idx);
	void RemoveBlockAtIdx(int idx);
	void SetQuestID(int id);
	void SetQuestName(std::string name);

	void PromoteBlock(int idx);
	void DemoteBlock(int idx);

	int GetQuestID();

	std::string GetQuestFragment();

private:

	int questID;
	std::string questName;
	int numBlocks;
	std::vector<QuestActionBlock*> blocks;

};