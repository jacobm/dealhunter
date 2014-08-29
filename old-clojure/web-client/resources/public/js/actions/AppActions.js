var AppDispatcher = require('../dispatcher/AppDispatcher');
var Constants = require('../constants/AppConstants');

var AppActions = {

    search: function(text) {
	AppDispatcher.handleViewAction({
	    actionType: Constants.APP_SEARCH,
	    text: text
	});
    },
    
    loginUser: function(authResult) {
	AppDispatcher.handleViewAction({
	    actionType: Constants.LOGIN_USER,
	    authResult: authResult
	});
    }
};

module.exports = AppActions;
