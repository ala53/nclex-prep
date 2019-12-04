var categoryNames = [];
var questionCount = 0;
var numCorrect = 0;
var answerDOMElements = [];
var currentQuestion = null;
var orderedHistoryArray = [];

function startTest() {
    //Mark questions active
    markQuestionsActive();
    //Choose a random category and question to start with
    chooseQuestion();

}

function chooseQuestion() {
    var q;
    if (adaptiveTest) {
        //Don't be fancy early in the test
        var activeCats = [];
        //Get all active categories
        categories.forEach(cat => {
            if (cat.alive)
                activeCats.push(cat);
        });
        //Compute probabilities
        activeCats.forEach(cat => computeWeightedProb(cat, activeCats))
        //Choose Category
        var category = selectWeightedProb(activeCats)
        //Choose Subcategory
        var activeSub = []
        category.children.forEach(cat => {
            if (cat.alive)
                activeSub.push(cat);
        });
        activeSub.forEach(subCat => computeWeightedProb(subCat, activeSub));

        console.log("Question Decision:");
        activeCats.forEach(cat => console.log(cat.name + ": p=" + cat.probability + " (" + cat.correctQCount + "/" + cat.completedQCount + ")"));
        console.log("Chose category: " + category.name)
        activeSub.forEach(cat => console.log("\t" + cat.name + ": p=" + cat.probability + " (" + cat.correctQCount + "/" + cat.completedQCount + ")"));

        var sub = selectWeightedProb(activeSub);
        //Choose random q
        sub.questions.forEach(question => {
            if (q == undefined && question.alive) {
                q = question;
                q.alive = false;
                return;
            }
        });

    }
    else {
        //Not an adaptive test, just choose efficiently and randomly
        q = randomQ();
    }
    currentQuestion = q;
    renderQuestion(q);
    recomputeLists();
}

function computeWeightedProb(cat, activeCats) {
    var baseCount = activeCats.length * 2;
    var oneItem = 2;
    if (cat.completedQCount < 3) {
        cat.probability = oneItem;
    }
    else {
        var successRate = cat.correctQCount / cat.completedQCount;
        if (successRate > 0.8) { //Low probability because high success
            cat.probability = oneItem / 2;
        }
        else if (successRate > 0.7) {
            cat.probability = Math.ceil(oneItem);
        }
        else if (successRate > 0.6) {
            cat.probability = Math.ceil(oneItem * 1.5);
        }
        else if (successRate > 0.5) {
            cat.probability = Math.ceil(oneItem * 2);
        }
        else if (successRate > 0.4) {
            cat.probability = Math.ceil(oneItem * 2.5);
        }
        else if (successRate > 0.3) {
            cat.probability = Math.ceil(oneItem * 3);
        }
        else if (successRate > 0.2) {
            cat.probability = Math.ceil(oneItem * 3.5);
        }
        else if (successRate > 0.1) {
            cat.probability = Math.ceil(oneItem * 4);
        }
        else { // highest probability
            cat.probability = Math.ceil(Math.max(baseCount * 0.8, oneItem * 5));
        }
    }
}

function selectWeightedProb(activeCats) {
    catsNew = []
    activeCats.forEach(cat => {
        for (var i = 0; i < cat.probability; i++)
            catsNew.push(cat);
    })
    return arrayRandom(catsNew)
}

function randomQ() {

    var activeCats = [];
    //Get all active categories
    categories.forEach(cat => {
        if (cat.alive)
            activeCats.push(cat);
    });
    //Choose one
    var category = arrayRandom(activeCats);
    //Get all active subcats
    var activeSub = [];
    category.children.forEach(cat => {
        if (cat.alive)
            activeSub.push(cat);
    });
    //choose one
    var sub = arrayRandom(activeSub);
    //choose first active question in it
    var q = undefined;
    sub.questions.forEach(question => {
        if (q == undefined && question.alive) {
            q = question;
            q.alive = false;
            return;
        }
    });

    return q;
}

//Recalculates "alive status"
function recomputeLists() {
    categories.forEach(cat => {
        var livingCatCount = 0;
        cat.children.forEach(child => {
            var livingQCount = 0;
            child.questions.forEach(question => {
                if (question.completed)
                    question.alive = false;
                if (question.alive)
                    livingQCount++;
            });
            if (livingQCount > 0) {
                child.alive = true;
                livingCatCount++;
            }
        })
        if (livingCatCount > 0) {
            cat.alive = true;
        }
    })

}

function renderQuestion(question) {
    answerDOMElements = [];
    var qText = document.getElementById("question_text");
    var aList = document.getElementById("answer_list");
    aList.innerHTML = "";
    var rationale = document.getElementById("rationale");
    var source = document.getElementById("source_url");
    var status = document.getElementById("test_status");
    rationale.style = "visibility:hidden";

    questionCount++;


    qText.innerText = question.Question;
    source.href = question.SourceUrl;
    rationale.innerText = question.Rationale;
    if (showAnswer) {
        var score = 0;
        if (questionCount > 1)
            score = ((numCorrect / (questionCount - 1)) * 100).toFixed(1).toString();
        status.innerText = "Question " + questionCount + ". Score: " + (score) + "%. Category: " + question.parent.parent.name + " / " + question.parent.name;
    }
    else {
        status.innerText = "Question " + questionCount;
    }
    if (question.IsSelectAll) {
        status.innerText = "Select All That Apply. " + status.innerText;
        //Create radio buttons
        var ansNumber = 0;
        question.Answers.forEach(ans => {
            var parent = document.createElement("li");
            var elem = document.createElement("input");
            elem.type = "checkbox";
            parent.appendChild(elem);
            var span = document.createElement("span");
            span.innerText = ans;
            var isCorrect = false;
            question.Correct.forEach(corr => {
                if (corr == ansNumber)
                    isCorrect = true;
            })
            elem.isCorrectAnswer = isCorrect;
            parent.appendChild(span);
            ansNumber++;

            aList.appendChild(parent);
            answerDOMElements.push(elem);
        });
    }
    else {
        //Create radio buttons
        var ansNumber = 0;
        question.Answers.forEach(ans => {
            var parent = document.createElement("li");
            var elem = document.createElement("input");
            elem.type = "radio";
            elem.name = "radioanswer";
            parent.appendChild(elem);
            var span = document.createElement("span");
            span.innerText = ans;
            if (ansNumber == question.Correct[0]) {
                elem.isCorrectAnswer = true;
            }
            else elem.isCorrectAnswer = false;
            parent.appendChild(span);
            ansNumber++;

            aList.appendChild(parent);
            answerDOMElements.push(elem);
        });
    }
}

var isOnRationaleScreen = false;
function onNextQuestionButtonPress() {
    if (isOnRationaleScreen) {
        isOnRationaleScreen = false;
        document.getElementById("next_question_btn").innerText = "Submit Answer";
        chooseQuestion();
        return;
    }

    //Check if an answer was selected
    var ansSelected = false;
    answerDOMElements.forEach(itm => {
        if (itm.checked) ansSelected = true;
    })
    if (!ansSelected) {
        alert("No answer was chosen.");
        return;
    }

    //Switch to answer screen
    isOnRationaleScreen = true;
    document.getElementById("next_question_btn").innerText = "Next Question";

    currentQuestion.completed = true;
    var wasCorrect = true;
    var chosenAnswers = [];
    var ansNum = 0;
    answerDOMElements.forEach(ans => {
        ans.enabled = false;
        if (ans.checked) chosenAnswers.push(ansNum);
        if (ans.isCorrectAnswer && ans.checked) return;
        if (!ans.isCorrectAnswer && !ans.checked) return;
        wasCorrect = false;
        ansNum++;
    });
    //evaluate answers
    answerDOMElements.forEach(ans => {

        if (ans.isCorrectAnswer) {
            ans.parentElement.style = "background-color:green";
        }
        if (!wasCorrect)
            if (!ans.isCorrectAnswer && ans.checked) {
                ans.parentElement.style = "background-color:red";
            }
    })

    if (wasCorrect)
        numCorrect++;

    var aList = document.getElementById("answer_list");
    var rationale = document.getElementById("rationale");
    var source = document.getElementById("source_url");
    var status = document.getElementById("test_status");
    if (showAnswer) {
        if (wasCorrect) rationale.innerText = "Answered Correctly. " + currentQuestion.Rationale;
        if (!wasCorrect) rationale.innerText = "Incorrectly Answered. " + currentQuestion.Rationale;
        rationale.style = "visibility:visible";
    }

    currentQuestion.chosenAnswers = chosenAnswers;
    currentQuestion.completed = true;
    currentQuestion.wasCorrect = wasCorrect;
    currentQuestion.parent.completedQCount++;
    currentQuestion.parent.parent.completedQCount++;
    if (wasCorrect) {
        currentQuestion.parent.correctQCount++;
        currentQuestion.parent.parent.correctQCount++;
    }
    orderedHistoryArray.push(currentQuestion);
}

function shuffleArray(array) {
    var currentIndex = array.length, temporaryValue, randomIndex;

    // While there remain elements to shuffle...
    while (0 !== currentIndex) {

        // Pick a remaining element...
        randomIndex = Math.floor(Math.random() * currentIndex);
        currentIndex -= 1;

        // And swap it with the current element.
        temporaryValue = array[currentIndex];
        array[currentIndex] = array[randomIndex];
        array[randomIndex] = temporaryValue;
    }

    return array;
}

function arrayRandom(items) {
    return items[Math.floor(Math.random() * items.length)];
}

function endTestButton() {
    if (confirm("Are you sure you want to end the test?")) {
        document.getElementById("test_screen").remove();
        document.getElementById("test_completed").style = "";
        var qc = orderedHistoryArray.length;
        if (qc == 0)
            document.getElementById("num_correct").innerText = "0";
        else
            document.getElementById("num_correct").innerText = (((numCorrect / qc) * 100).toFixed(1)) + "%. " + numCorrect + "/" + qc;

        var ansDiv = document.getElementById(answerDiv);
        //Print all questions

        orderedHistoryArray.forEach(question => {
            console.log(question);
            var div = document.createElement("div");
            //Print the question
            var qq = document.createElement("h4");
            var corrx = "";
            if (question.wasCorrect)
                corrx = "Correct: ";
            else corrx = "Incorrect: ";

            qq.innerText = corrx + question.Question;
            div.appendChild(qq);
            //Print the answers
            for (var i = 0; i < question.Answers.length; i++) {
                var selected = question.chosenAnswers.includes(i);
                var correct = question.Correct.includes(i);
                var cor = "";
                var ans = document.createElement("div");
                if (correct) {
                    ans.style = "color:black; font-weight: bold;";
                    cor = " (correct answer)";
                }
                else ans.style = "color:grey";
                if (selected)
                    ans.innerText = "☑ " + question.Answers[i] + cor;
                else ans.innerText = "☐ " + question.Answers[i] + cor;

                div.appendChild(ans);
            }
            if (question.Rationale.length > 1) {
                var rationale = document.createElement("h4");
                rationale.innerText = "Rationale: " + question.Rationale;
                div.appendChild(rationale);
            }
            div.appendChild(document.createElement("hr"));
            answerDiv.appendChild(div);

        })
        //Push the question array to the server here
    }

}

//Looks through question list and adds a "active" property to all questions that could be used during test
//Also shuffles the question arrays, adds a completed property to each one.
function markQuestionsActive() {
    categories.forEach(cat => {
        var qCount = 0;
        var subCatNames = [];

        cat.children.forEach(subCat => {
            var scQcount = 0;
            //Shuffle the question array
            subCat.questions = shuffleArray(subCat.questions);
            subCat.questions.forEach(question => {
                if (false) return; //TODO check if already seen
                if (question.Rationale.length < 1 && rationaleOnly) return; //Skip non rationale when rationale-only mode is on
                if (!subCat.active) return; //Skip inactive subcategories
                question.alive = true;
                question.parent = subCat;
                qCount++;
                scQcount++;
            });

            if (scQcount > 0) {
                subCatNames.push(subCat.name);
                //Track # of completed questions
                subCat.questionCount = scQcount;
                subCat.completedQCount = 0;
                subCat.correctQCount = 0;
                subCat.alive = true;
            }
        })


        if (qCount > 0) {
            //Add to the list only if it has available questions
            categoryNames.push(cat.name);
            cat.alive = true;
            cat.questionCount = qCount;
            cat.completedQCount = 0;
            cat.correctQCount = 0;
        }
    })

}