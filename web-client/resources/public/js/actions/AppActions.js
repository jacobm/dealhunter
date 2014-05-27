var AppDispatcher = require('../dispatcher/AppDispatcher');
var Constants = require('../constants/AppConstants');

var AppActions = {

    search: function(text) {
	AppDispatcher.handleViewAction({
	    actionType: Constants.APP_SEARCH,
	    text: text
	});
    }

};

module.exports = AppActions;
