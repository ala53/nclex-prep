var database = {};
var categories = [];
var rationaleOnly = false;
var adaptiveTest = true;
var hideSeen = false;
var showAnswer = true;
var qCountLabel = null;


//class definitions
function category(catName, attachedObj) {
    this.name = catName;
    this.attachedObj = attachedObj;
    this.children = [];
}
function subCategory(catName, parent, questions) {
    this.name = catName;
    this.parent = parent;
    this.questions = questions;
    this.active = false;
}

//Code to handle checkboxes for test options
function rationaleOnlyQs() {
    rationaleOnly = document.getElementById("only_rationale").checked;
    generateCheckboxes();
}

function hideSeenQs() {
    hideSeen = document.getElementById("hide_seen").checked;
    generateCheckboxes();
}

function showAnswerQs() {
    showAnswer = document.getElementById("show_answers").checked;
}

function adaptiveTestQs() {
    adaptiveTest = document.getElementById("adaptive_test").checked;
}

function startTestBtn() {
    document.getElementById("home_screen_page").remove();
    document.getElementById("test_screen").style = "";
    startTest();
}

//Download the database CSV
function downloadDatabase() {
    xhr = new XMLHttpRequest(),
        method = "GET",
        url = "question_db.json";

    xhr.open(method, url, true);
    xhr.onreadystatechange = function () {
        if (xhr.readyState === XMLHttpRequest.DONE) {
            if (xhr.status == 200) {
                try {
                    database = JSON.parse(xhr.responseText);
                    document.getElementById("status_label").innerText = "Database Loaded.";
                    document.getElementById("start_test").style = "";
                    parseDatabase();
                    generateCheckboxes();
                }
                catch (e) {
                    document.getElementById("status_label").innerText = "Failed to Load Database!";
                    console.log(e);
                }
            }
            else {
                document.getElementById("status_label").innerText = "Failed to Load Database!";
            }
        }
    };
    xhr.send();
}


function parseDatabase() {
    var cats = Object.keys(database);
    //Add categories
    cats.forEach(element => {
        var cat = new category(element);
        categories.push(cat);
        //Add subcategories
        var subCats = Object.keys(database[element]);
        subCats.forEach(subElem => {
            var qList = database[element][subElem].Questions;
            var subCat = new subCategory(subElem, cat, qList);
            if (database[element][subElem].StartActive)
                subCat.active = true;
            cat.children.push(subCat);
        });
    });
}

function catOnClick(chkbox) {
    var elem = chkbox.srcElement;
    if (elem.checked) {
        elem.attachedObj.children.forEach(child => {
            child.active = true;
            if (child.checkbox != undefined)
                child.checkbox.checked = true;
        });
        chkbox.indeterminate = false;
    }
    else {
        elem.attachedObj.children.forEach(child => {
            child.active = false;
            if (child.checkbox != undefined)
                child.checkbox.checked = false;
        });
        chkbox.indeterminate = false;
    }

    recomputeQuestionCount();
}

function subCatOnClick(chkbox) {
    var elem = chkbox.srcElement;
    var childObj = elem.attachedObj;
    var parentObj = childObj.parent;

    childObj.active = elem.checked;

    console.log(childObj);

    if (elem.checked) {
        //Check if all others are checked
        var allChecked = true;
        parentObj.children.forEach(sub => {
            if (!sub.active)
                allChecked = false;
        });
        if (allChecked) {
            parentObj.checkbox.checked = true;
            parentObj.checkbox.indeterminate = false;
        }
        else {
            parentObj.checkbox.checked = true;
            parentObj.checkbox.indeterminate = true;
        }
    }
    else {
        var anyChecked = false;
        parentObj.children.forEach(sub => {
            if (sub.active)
                anyChecked = true;
        });

        if (anyChecked) {
            parentObj.checkbox.checked = true;
            parentObj.checkbox.indeterminate = true;
        }
        else {
            parentObj.checkbox.checked = false;
            parentObj.checkbox.indeterminate = false;
        }
    }

    recomputeQuestionCount();

}

function recomputeQuestionCount() {
    var qCount = 0;
    var totalQs = 0;
    categories.forEach(cat => {
        cat.children.forEach(subCat => {
            subCat.questions.forEach(question => {
                if (false) return; //TODO check if already seen
                if (question.Rationale.length < 1 && rationaleOnly) return; //Skip non rationale when rationale-only mode is on
                totalQs++;
                if (!subCat.active) return;
                qCount++;
            });
        })
    })

    if (totalQs == 0) {
        qCountLabel.innerText = "No questions available. Did you check hide already seen after completing all questions?";

    }
    else {
        if (qCount > 0) {
            qCountLabel.innerText = qCount + " of " + totalQs + " questions selected for test";
            document.getElementById("start_test").style = "";
        }
        else {
            qCountLabel.innerText = "No questions selected";
            document.getElementById("start_test").style = "visibility:hidden";
        }
    }
}

function generateCheckboxes() {
    var baseElem = document.getElementById("category_list");
    baseElem.innerHTML = "";
    var h3 = document.createElement("h3");
    qCountLabel = h3;
    var globalLi = document.createElement("li");
    globalLi.appendChild(h3);
    baseElem.appendChild(globalLi);

    categories.forEach(category => {
        var baseLi = document.createElement("li");
        var checkbox = document.createElement("input");
        var spanBase = document.createElement("span");
        var childList = document.createElement("ul");
        var questionCount = 0;

        checkbox.onclick = catOnClick;
        checkbox.type = "checkbox";
        checkbox.attachedObj = category;

        baseLi.appendChild(checkbox);
        baseLi.appendChild(spanBase);
        baseLi.appendChild(childList);

        var hasCategoryChecked = false;
        var hasAllCatsChecked = true;

        category.children.forEach(child => {

            //WARN if empty category and exit early
            if (child.questions.length == 0) {
                console.log("Empty Category: " + category.name + " / " + child.name);
                return;
            }

            var subQCount = 0;
            //question counting
            child.questions.forEach(question => {
                //Also here: validate for already seen TODO

                //Validate rationale present
                if (rationaleOnly) {
                    if (question.Rationale.length > 1)
                        subQCount++;
                    else return;
                }
                else subQCount++;
            });

            //Checked?
            if (child.active) {
                hasCategoryChecked = true;
            }
            else hasAllCatsChecked = false;

            questionCount += subQCount;
            //Add child
            if (subQCount > 0) {
                var subLi = document.createElement("li");
                var subChkbox = document.createElement("input");
                subChkbox.onclick = subCatOnClick;
                subChkbox.attachedObj = child;
                var subSpan = document.createElement("span");

                subSpan.innerText = child.name + " (" + subQCount + " questions)";
                if (child.active) {
                    subChkbox.checked = true;
                }
                child.checkbox = subChkbox;
                subChkbox.type = "checkbox";
                subLi.appendChild(subChkbox);
                subLi.appendChild(subSpan);
                childList.appendChild(subLi);


            }

        });
        if (questionCount > 0) {
            category.checkbox = checkbox;
            //Figure out if we should check or indeterminate or no
            if (hasCategoryChecked == false) {
                checkbox.checked = false;
            }
            else {
                if (hasAllCatsChecked) {
                    checkbox.checked = true;
                }
                else {
                    checkbox.checked = true;
                    checkbox.indeterminate = true;
                }
            }
            //Dont show categories with 0 questions
            spanBase.innerText = category.name + " (" + questionCount + " questions)";
            baseElem.appendChild(baseLi);



        }
    });

    recomputeQuestionCount();
}

downloadDatabase();